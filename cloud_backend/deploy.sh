#!/bin/bash
# ============================================================
# TempHumidityMonitor 云端后端部署脚本
# 目标服务器: 47.116.50.90
# 使用方法:
#   1. 将整个 cloud_backend 目录上传到服务器 /opt/thm-cloud/
#   2. 以 root 执行: bash /opt/thm-cloud/deploy.sh
# ============================================================
set -e

APP_DIR="/opt/thm-cloud"
APP_USER="thm"
APP_PORT=5000

echo "=== TempHumidityMonitor 云端部署 ==="

# ---- 1. 安装系统依赖 ----
echo "[1/6] 安装系统依赖..."
if command -v apt-get &>/dev/null; then
    apt-get update -qq
    apt-get install -y -qq python3 python3-pip python3-venv nginx
elif command -v yum &>/dev/null; then
    yum install -y python3 python3-pip nginx
else
    echo "不支持的包管理器，请手动安装 python3, pip, nginx"
    exit 1
fi

# ---- 2. 创建应用用户 ----
echo "[2/6] 创建应用用户..."
id -u "$APP_USER" &>/dev/null || useradd -r -s /bin/false "$APP_USER"

# ---- 3. 设置虚拟环境 ----
echo "[3/6] 设置 Python 虚拟环境..."
python3 -m venv "$APP_DIR/venv"
source "$APP_DIR/venv/bin/activate"
pip install --upgrade pip -q
pip install -r "$APP_DIR/requirements.txt" -q
deactivate

# ---- 4. 设置文件权限 ----
echo "[4/6] 设置文件权限..."
chown -R "$APP_USER:$APP_USER" "$APP_DIR"
chmod 755 "$APP_DIR"

# ---- 5. 配置 systemd 服务 ----
echo "[5/6] 配置 systemd 服务..."
cat > /etc/systemd/system/thm-cloud.service << 'SERVICE'
[Unit]
Description=TempHumidityMonitor Cloud Backend
After=network.target

[Service]
Type=simple
User=thm
Group=thm
WorkingDirectory=/opt/thm-cloud
ExecStart=/opt/thm-cloud/venv/bin/gunicorn -w 2 -k gevent -b 127.0.0.1:5000 app:app
Restart=always
RestartSec=5
Environment=DB_PATH=/opt/thm-cloud/sensor_data.db
Environment=DATA_RETAIN_DAYS=90

[Install]
WantedBy=multi-user.target
SERVICE

systemctl daemon-reload
systemctl enable thm-cloud
systemctl restart thm-cloud

# ---- 6. 配置 Nginx ----
echo "[6/6] 配置 Nginx..."
cat > /etc/nginx/sites-available/thm-cloud << 'NGINX'
server {
    listen 80;
    server_name 47.116.50.90;

    # 日志
    access_log /var/log/nginx/thm-cloud-access.log;
    error_log  /var/log/nginx/thm-cloud-error.log;

    # 静态文件
    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;

        # SSE 长连接支持
        proxy_buffering off;
        proxy_cache off;
        proxy_read_timeout 86400s;
        chunked_transfer_encoding on;
    }

    # API
    location /api/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_http_version 1.1;

        # SSE 禁用缓冲
        proxy_buffering off;
        proxy_cache off;
        proxy_read_timeout 86400s;
    }
}
NGINX

# 启用站点
if [ -d /etc/nginx/sites-enabled ]; then
    ln -sf /etc/nginx/sites-available/thm-cloud /etc/nginx/sites-enabled/thm-cloud
    # 移除默认站点
    rm -f /etc/nginx/sites-enabled/default
elif [ -d /etc/nginx/conf.d ]; then
    ln -sf /etc/nginx/sites-available/thm-cloud /etc/nginx/conf.d/thm-cloud.conf
fi

# 测试配置并重载
nginx -t && systemctl reload nginx

# ---- 完成 ----
echo ""
echo "============================================"
echo "  部署完成!"
echo "  面板地址: http://47.116.50.90"
echo "  查看日志: journalctl -u thm-cloud -f"
echo "  服务控制: systemctl [start|stop|restart] thm-cloud"
echo "============================================"
