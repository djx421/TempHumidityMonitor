"""
TempHumidityMonitor - 云端后端服务
Flask REST API + SSE 实时推送 + SQLite 数据存储
部署: gunicorn -w 2 -k gevent app:app
"""
import sqlite3
import json
import time
import threading
import os
from datetime import datetime
from flask import Flask, request, jsonify, Response, send_from_directory

# ============================================================
# Flask 初始化
# ============================================================
app = Flask(__name__, static_folder='web', static_url_path='')
DB_PATH = os.environ.get('DB_PATH', os.path.join(os.path.dirname(__file__), 'sensor_data.db'))
DATA_RETAIN_DAYS = int(os.environ.get('DATA_RETAIN_DAYS', '90'))

# ============================================================
# SSE 客户端管理
# ============================================================
_sse_clients = []
_sse_lock = threading.Lock()

# ============================================================
# 内存最新数据（线程安全）
# ============================================================
_data_lock = threading.Lock()
_latest = {
    'temperature': 0.0, 'humidity': 0.0, 'pressure': 101.3,
    'timestamp': '', 'isSimulated': False, 'isReading': False,
    'mode': 'offline', 'statusText': '等待数据...'
}
_thresholds = {
    'tempHi': 40.0, 'tempLo': 0.0,
    'humiHi': 80.0, 'humiLo': 20.0,
    'pressHi': 110.0, 'pressLo': 90.0,
    'alarmEnabled': True
}
_counters = {'send': 0, 'recv': 0, 'error': 0}
_alarm_count = 0
_data_count = 0
_stats = {
    'tempMin': None, 'tempMax': None, 'tempSum': 0.0,
    'humiMin': None, 'humiMax': None, 'humiSum': 0.0,
    'pressMin': None, 'pressMax': None, 'pressSum': 0.0
}

# ============================================================
# 数据库初始化
# ============================================================
def init_db():
    conn = sqlite3.connect(DB_PATH)
    conn.execute('''CREATE TABLE IF NOT EXISTS sensor_data (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        timestamp TEXT NOT NULL,
        temperature REAL, humidity REAL, pressure REAL,
        source TEXT DEFAULT 'hardware',
        is_alarm INTEGER DEFAULT 0,
        alarm_msg TEXT DEFAULT ''
    )''')
    conn.execute('CREATE INDEX IF NOT EXISTS idx_ts ON sensor_data(timestamp)')
    conn.commit()
    conn.close()

def clean_old_data():
    """清理超过保留天数的旧数据"""
    try:
        cutoff = datetime.now().replace(hour=0, minute=0, second=0, microsecond=0)
        from datetime import timedelta
        cutoff -= timedelta(days=DATA_RETAIN_DAYS)
        conn = sqlite3.connect(DB_PATH)
        conn.execute('DELETE FROM sensor_data WHERE timestamp < ?', (cutoff.isoformat(),))
        conn.commit()
        conn.close()
    except Exception:
        pass

init_db()

# ============================================================
# SSE 广播
# ============================================================
def broadcast_sse(data):
    """向所有 SSE 客户端推送实时数据"""
    payload = f"event: data\ndata: {json.dumps(data, ensure_ascii=False)}\n\n"
    with _sse_lock:
        dead = []
        for i, w in enumerate(_sse_clients):
            try:
                w(payload)
            except Exception:
                dead.append(i)
        for i in reversed(dead):
            _sse_clients.pop(i)

# ============================================================
# CORS 支持
# ============================================================
@app.after_request
def after_request(response):
    response.headers['Access-Control-Allow-Origin'] = '*'
    response.headers['Access-Control-Allow-Methods'] = 'GET, POST, OPTIONS'
    response.headers['Access-Control-Allow-Headers'] = 'Content-Type'
    return response

# ============================================================
# 静态文件
# ============================================================
@app.route('/')
def index():
    return send_from_directory('web', 'index.html')

@app.route('/<path:path>')
def static_files(path):
    return send_from_directory('web', path)

# ============================================================
# API: 接收本地 C# 程序推送的数据
# ============================================================
@app.route('/api/data', methods=['POST'])
def api_data():
    """POST 传感器数据，由本地 C# 程序调用"""
    d = request.get_json(silent=True)
    if not d:
        return jsonify({'error': 'invalid json'}), 400

    t = float(d.get('temperature', 0))
    h = float(d.get('humidity', 0))
    p = float(d.get('pressure', 101.3))
    ts = d.get('timestamp', datetime.now().isoformat())
    is_sim = d.get('isSimulated', False)
    mode_str = 'simulation' if is_sim else 'hardware'

    global _data_count, _alarm_count
    with _data_lock:
        _latest.update({
            'temperature': t, 'humidity': h, 'pressure': p,
            'timestamp': ts, 'isSimulated': is_sim, 'isReading': True,
            'mode': mode_str, 'statusText': d.get('statusText', f'{mode_str} running')
        })
        _data_count += 1
        _stats['tempSum'] += t; _stats['humiSum'] += h; _stats['pressSum'] += p
        if _stats['tempMin'] is None or t < _stats['tempMin']: _stats['tempMin'] = t
        if _stats['tempMax'] is None or t > _stats['tempMax']: _stats['tempMax'] = t
        if _stats['humiMin'] is None or h < _stats['humiMin']: _stats['humiMin'] = h
        if _stats['humiMax'] is None or h > _stats['humiMax']: _stats['humiMax'] = h
        if _stats['pressMin'] is None or p < _stats['pressMin']: _stats['pressMin'] = p
        if _stats['pressMax'] is None or p > _stats['pressMax']: _stats['pressMax'] = p
        _counters['send'] = int(d.get('sendCount', _counters['send']))
        _counters['recv'] = int(d.get('recvCount', _counters['recv']))
        _counters['error'] = int(d.get('errorCount', _counters['error']))

    # 报警检测
    alarm_msg = ''
    is_alarm = 0
    th = _thresholds
    if th['alarmEnabled']:
        parts = []
        if t > th['tempHi']: parts.append(f'温度高:{t:.1f}')
        if t < th['tempLo']: parts.append(f'温度低:{t:.1f}')
        if h > th['humiHi']: parts.append(f'湿度高:{h:.1f}')
        if h < th['humiLo']: parts.append(f'湿度低:{h:.1f}')
        if p > th['pressHi']: parts.append(f'气压高:{p:.1f}')
        if p < th['pressLo']: parts.append(f'气压低:{p:.1f}')
        if parts:
            is_alarm = 1
            alarm_msg = '; '.join(parts)
            _alarm_count += 1

    # 写入数据库
    try:
        conn = sqlite3.connect(DB_PATH)
        conn.execute(
            'INSERT INTO sensor_data (timestamp, temperature, humidity, pressure, source, is_alarm, alarm_msg) '
            'VALUES (?,?,?,?,?,?,?)',
            (ts, t, h, p, mode_str, is_alarm, alarm_msg)
        )
        conn.commit()
        conn.close()
    except Exception:
        pass

    # 广播 SSE
    broadcast_sse({
        'temperature': t, 'humidity': h, 'pressure': p,
        'timestamp': ts, 'isSimulated': is_sim
    })

    return jsonify({'ok': True, 'isAlarm': bool(is_alarm), 'alarmMsg': alarm_msg})

# ============================================================
# API: 当前数据
# ============================================================
@app.route('/api/current')
def api_current():
    with _data_lock:
        return jsonify(_latest)

# ============================================================
# API: 历史数据查询
# ============================================================
@app.route('/api/history')
def api_history():
    start = request.args.get('start', datetime.now().replace(hour=0, minute=0, second=0).isoformat())
    end = request.args.get('end', datetime.now().isoformat())
    alarm_only = request.args.get('alarmOnly', '0') in ('1', 'true')
    limit = min(int(request.args.get('limit', '5000')), 10000)

    try:
        conn = sqlite3.connect(DB_PATH)
        conn.row_factory = sqlite3.Row
        where = 'WHERE timestamp BETWEEN ? AND ?'
        params = [start, end]
        if alarm_only:
            where += ' AND is_alarm=1'
        rows = conn.execute(
            f'SELECT timestamp, temperature, humidity, pressure, source, is_alarm, alarm_msg '
            f'FROM sensor_data {where} ORDER BY timestamp DESC LIMIT ?',
            params + [limit]
        ).fetchall()
        conn.close()

        result = []
        for r in rows:
            result.append({
                'timestamp': r['timestamp'],
                'temperature': round(r['temperature'], 1) if r['temperature'] else 0,
                'humidity': round(r['humidity'], 1) if r['humidity'] else 0,
                'pressure': round(r['pressure'], 1) if r['pressure'] else 0,
                'source': r['source'] or 'hardware',
                'isAlarm': bool(r['is_alarm']),
                'alarmMsg': r['alarm_msg'] or ''
            })
        return jsonify(result)
    except Exception as e:
        return jsonify({'error': str(e)}), 500

# ============================================================
# API: 报警状态
# ============================================================
@app.route('/api/alarms')
def api_alarms():
    with _data_lock:
        t, h, p = _latest['temperature'], _latest['humidity'], _latest['pressure']
        th = _thresholds
    alarm_enabled = th['alarmEnabled']
    temp_high = alarm_enabled and t > th['tempHi']
    temp_low = alarm_enabled and t < th['tempLo']
    humi_high = alarm_enabled and h > th['humiHi']
    humi_low = alarm_enabled and h < th['humiLo']
    press_high = alarm_enabled and p > th['pressHi']
    press_low = alarm_enabled and p < th['pressLo']
    return jsonify({
        'tempHigh': temp_high, 'tempLow': temp_low,
        'humiHigh': humi_high, 'humiLow': humi_low,
        'pressHigh': press_high, 'pressLow': press_low,
        'any': temp_high or temp_low or humi_high or humi_low or press_high or press_low
    })

# ============================================================
# API: 系统状态
# ============================================================
@app.route('/api/status')
def api_status():
    with _data_lock:
        connected = _latest['mode'] in ('hardware', 'simulation')
        mode = _latest['mode']
        is_reading = _latest['isReading']
        status_text = _latest['statusText']
        send_c = _counters['send']
        recv_c = _counters['recv']
        err_c = _counters['error']
    return jsonify({
        'connected': connected,
        'mode': mode if connected else 'offline',
        'isReading': is_reading,
        'isReconnecting': False,
        'statusText': status_text,
        'alarmCount': _alarm_count,
        'sendCount': send_c, 'recvCount': recv_c, 'errorCount': err_c
    })

# ============================================================
# API: 统计数据
# ============================================================
@app.route('/api/stats')
def api_stats():
    with _data_lock:
        count = _data_count
        if count == 0:
            return jsonify({'count': 0, 'tempMin': 0, 'tempMax': 0, 'tempAvg': 0,
                           'humiMin': 0, 'humiMax': 0, 'humiAvg': 0,
                           'pressMin': 0, 'pressMax': 0, 'pressAvg': 0})
        s = _stats
    return jsonify({
        'count': count,
        'tempMin': round(s['tempMin'], 1), 'tempMax': round(s['tempMax'], 1),
        'tempAvg': round(s['tempSum'] / count, 1),
        'humiMin': round(s['humiMin'], 1), 'humiMax': round(s['humiMax'], 1),
        'humiAvg': round(s['humiSum'] / count, 1),
        'pressMin': round(s['pressMin'], 1), 'pressMax': round(s['pressMax'], 1),
        'pressAvg': round(s['pressSum'] / count, 1)
    })

# ============================================================
# API: 阈值配置
# ============================================================
@app.route('/api/thresholds', methods=['GET', 'POST'])
def api_thresholds():
    if request.method == 'POST':
        d = request.get_json(silent=True)
        if d:
            with _data_lock:
                _thresholds.update({
                    'tempHi': float(d.get('tempHi', _thresholds['tempHi'])),
                    'tempLo': float(d.get('tempLo', _thresholds['tempLo'])),
                    'humiHi': float(d.get('humiHi', _thresholds['humiHi'])),
                    'humiLo': float(d.get('humiLo', _thresholds['humiLo'])),
                    'pressHi': float(d.get('pressHi', _thresholds['pressHi'])),
                    'pressLo': float(d.get('pressLo', _thresholds['pressLo'])),
                    'alarmEnabled': bool(d.get('alarmEnabled', _thresholds['alarmEnabled']))
                })
        return jsonify({'ok': True})

    with _data_lock:
        th = dict(_thresholds)
    return jsonify({
        'tempHi': th['tempHi'], 'tempLo': th['tempLo'],
        'humiHi': th['humiHi'], 'humiLo': th['humiLo'],
        'pressHi': th['pressHi'], 'pressLo': th['pressLo'],
        'alarmEnabled': th['alarmEnabled']
    })

# ============================================================
# SSE 实时流
# ============================================================
@app.route('/api/stream')
def api_stream():
    def generate():
        q = []
        with _sse_lock:
            _sse_clients.append(lambda msg: q.append(msg))
        try:
            yield f"event: connected\ndata: {json.dumps({'status': 'ok'})}\n\n"
            # 发送当前最新数据
            with _data_lock:
                cur = dict(_latest)
            yield f"event: data\ndata: {json.dumps(cur, ensure_ascii=False)}\n\n"
            while True:
                if q:
                    while q:
                        yield q.pop(0)
                else:
                    yield ": keepalive\n\n"
                    time.sleep(5)
        except GeneratorExit:
            pass
        finally:
            with _sse_lock:
                try:
                    idx = next(i for i, w in enumerate(_sse_clients) if hasattr(w, '_sentinel'))
                    _sse_clients.pop(idx)
                except StopIteration:
                    pass
    return Response(generate(), mimetype='text/event-stream',
                    headers={'Cache-Control': 'no-cache', 'Connection': 'keep-alive',
                             'X-Accel-Buffering': 'no'})

# ============================================================
# 定期清理
# ============================================================
def schedule_cleanup():
    """每天清理一次旧数据"""
    clean_old_data()
    threading.Timer(86400, schedule_cleanup).start()

# ============================================================
# 主入口
# ============================================================
if __name__ == '__main__':
    import sys
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 5000
    print(f'[TempHumidityMonitor Cloud] starting on port {port}')
    threading.Timer(60, schedule_cleanup).start()  # 启动后 60s 开始首次清理
    app.run(host='0.0.0.0', port=port, debug=False, threaded=True)
