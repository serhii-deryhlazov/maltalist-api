#!/bin/bash

set -e

# 1. Copy check-dotnet-maltalist.sh to /usr/local/bin
sudo cp "$(dirname "$0")/check-dotnet-maltalist.sh" /usr/local/bin/
sudo chmod +x /usr/local/bin/check-dotnet-maltalist.sh

# 2. Add cron job if not already present
CRON_JOB="*/1 * * * * bash /usr/local/bin/check-dotnet-maltalist.sh"
( crontab -l 2>/dev/null | grep -Fv "$CRON_JOB" ; echo "$CRON_JOB" ) | crontab -

# 3. Copy SSL files
sudo mkdir -p /var/ssl/api.maltalist.webnester.com
sudo cp "$(dirname "$0")/../ssl/"* /var/ssl/api.maltalist.webnester.com/

# 4. Create nginx config
sudo tee /etc/nginx/sites-available/maltalist-api > /dev/null <<'EOF'
# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name api.maltalist.webnester.com;

    # Redirect to HTTPS
    return 301 https://$host$request_uri;
}

# HTTPS server
server {
    listen 443 ssl;
    server_name api.maltalist.webnester.com;

    error_log /var/log/nginx/api_error.log debug;

    ssl_certificate /var/ssl/api.maltalist.webnester.com/certificate.crt;
    ssl_certificate_key /var/ssl/api.maltalist.webnester.com/private.key;

    location / {
        proxy_pass http://localhost:5023;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF

# 5. Enable site
sudo ln -sf /etc/nginx/sites-available/maltalist-api /etc/nginx/sites-enabled/

# 6. Test nginx config
if sudo nginx -t; then
    # 7. Reload nginx if config test is successful
    sudo systemctl reload nginx
    echo "Nginx reloaded successfully."
else
    echo "Nginx config test failed. Please check the configuration."
    exit 1