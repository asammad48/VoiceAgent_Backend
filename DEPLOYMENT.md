# VoiceAgent — VPS Deployment Guide

Single-domain deployment: frontend at `/`, backend at `/api`, both served by Nginx on the same host.  
No Docker required — .NET 8 runs directly via `systemd`.

---

## Architecture

```
Browser
  │
  ▼
Nginx (port 443 / 80)  yourdomain.com
  ├── /api/*            → localhost:5010  (ASP.NET Core API)
  ├── /api/voice/*      → localhost:5010  (WebSocket — voice streaming)
  ├── /swagger          → localhost:5010  (Swagger UI)
  └── /*                → /var/www/voiceagent/frontend  (React SPA)

localhost:5010  →  PostgreSQL (voice_agent DB)
```

---

## Prerequisites on VPS

- Ubuntu 20.04 / 22.04
- .NET 8 runtime installed
- Nginx installed
- PostgreSQL installed and running
- A non-root user with passwordless `sudo systemctl` access (see Step 3)

---

## One-Time VPS Setup

### Step 1 — Install .NET 8 runtime (if not already installed)

```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update && sudo apt install -y aspnetcore-runtime-8.0
```

### Step 2 — Create PostgreSQL database

```bash
sudo -u postgres psql
```

```sql
CREATE USER va_user WITH PASSWORD 'YOUR_STRONG_DB_PASSWORD';
CREATE DATABASE voice_agent OWNER va_user;
GRANT ALL PRIVILEGES ON DATABASE voice_agent TO va_user;
\q
```

### Step 3 — Create deployment folders and log directory

```bash
sudo mkdir -p /var/www/voiceagent/api
sudo mkdir -p /var/www/voiceagent/frontend
sudo mkdir -p /var/log/voiceagent

sudo chown -R www-data:www-data /var/www/voiceagent
sudo chown -R www-data:www-data /var/log/voiceagent
```

### Step 4 — Create the secrets environment file

This file holds all sensitive values and **never goes into git**.  
The systemd service reads it at startup.

```bash
sudo mkdir -p /etc/voiceagent
sudo nano /etc/voiceagent/api.env
```

Paste and fill in your real values:

```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=voice_agent;Username=va_user;Password=YOUR_STRONG_DB_PASSWORD

# AI providers
Gemini__ApiKey=YOUR_GEMINI_API_KEY
Deepgram__ApiKey=YOUR_DEEPGRAM_API_KEY
ElevenLabs__ApiKey=YOUR_ELEVENLABS_API_KEY
ElevenLabs__DefaultVoiceId=nPczCjzI2devNBz1zQrb

# Telephony (leave blank if not using)
Telnyx__ApiKey=
Telnyx__ConnectionId=
Telnyx__FromNumber=

# Cloudflare R2 (leave blank if call recording is disabled)
CloudflareR2__Endpoint=
CloudflareR2__Bucket=voice-agent-recordings
CloudflareR2__AccessKey=
CloudflareR2__SecretKey=
```

Lock down the file:

```bash
sudo chmod 600 /etc/voiceagent/api.env
sudo chown root:root /etc/voiceagent/api.env
```

### Step 5 — Create the systemd service

```bash
sudo nano /etc/systemd/system/voiceagent-api.service
```

```ini
[Unit]
Description=VoiceAgent API
After=network.target postgresql.service

[Service]
WorkingDirectory=/var/www/voiceagent/api
ExecStart=/usr/bin/dotnet /var/www/voiceagent/api/VoiceAgent.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=voiceagent-api
User=www-data
Group=www-data

# Runtime environment
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5010
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Secrets — loaded from the env file, never stored in git
EnvironmentFile=/etc/voiceagent/api.env

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable voiceagent-api
sudo systemctl start voiceagent-api
sudo systemctl status voiceagent-api
```

### Step 6 — Allow deploy user to restart the service without a password

The GitHub Actions workflow SSH-es as your deploy user and runs `sudo systemctl restart`.  
Grant just that permission:

```bash
sudo visudo -f /etc/sudoers.d/voiceagent-deploy
```

Add this line (replace `deployuser` with your actual SSH username):

```
deployuser ALL=(ALL) NOPASSWD: /bin/systemctl restart voiceagent-api, /bin/systemctl is-active voiceagent-api, /usr/bin/journalctl, /usr/sbin/nginx, /bin/systemctl reload nginx
```

### Step 7 — Configure Nginx

Create a new site config — **do not edit any existing Nginx config**:

```bash
sudo nano /etc/nginx/sites-available/voiceagent
```

```nginx
# ── HTTP → HTTPS redirect (Certbot fills this in) ───────────────────────────
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$host$request_uri;
}

# ── Main site ────────────────────────────────────────────────────────────────
server {
    listen 443 ssl;
    server_name yourdomain.com;

    # SSL — Certbot manages these lines
    # ssl_certificate     /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    # ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;

    # ── Voice WebSocket (must be before /api/ block) ──────────────────────────
    location /api/voice/ {
        proxy_pass         http://localhost:5010;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection "Upgrade";
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_read_timeout 3600s;
    }

    # ── REST API ──────────────────────────────────────────────────────────────
    location /api/ {
        proxy_pass         http://localhost:5010;
        proxy_http_version 1.1;
        proxy_set_header   Connection "";
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_read_timeout 300s;
        proxy_connect_timeout 10s;
        client_max_body_size 10m;
    }

    # ── Swagger UI ────────────────────────────────────────────────────────────
    location /swagger {
        proxy_pass         http://localhost:5010;
        proxy_http_version 1.1;
        proxy_set_header   Host $host;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }

    # ── Frontend SPA ──────────────────────────────────────────────────────────
    root /var/www/voiceagent/frontend;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Static asset caching
    location ~* \.(js|css|png|jpg|jpeg|gif|svg|ico|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

Enable and test:

```bash
sudo ln -s /etc/nginx/sites-available/voiceagent /etc/nginx/sites-enabled/voiceagent
sudo nginx -t
sudo systemctl reload nginx
```

### Step 8 — SSL with Certbot

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d yourdomain.com
```

Certbot edits only the `voiceagent` config. Existing sites are untouched.

---

## GitHub Secrets

Go to each GitHub repo → **Settings → Secrets and Variables → Actions → New repository secret**.

Add these four secrets to **both** the backend repo and the frontend repo:

| Secret | Value |
|---|---|
| `VPS_HOST` | Your VPS IP address or hostname |
| `VPS_USER` | SSH username (the deploy user from Step 6) |
| `VPS_SSH_KEY` | Contents of your private SSH key (`~/.ssh/id_rsa`) |
| `VPS_PORT` | SSH port (usually `22`) |

**To generate an SSH key pair for deployment (if you don't have one):**

```bash
ssh-keygen -t ed25519 -C "voiceagent-deploy" -f ~/.ssh/voiceagent_deploy
# Add the public key to VPS
ssh-copy-id -i ~/.ssh/voiceagent_deploy.pub deployuser@your-vps-ip
# Paste the private key contents into VPS_SSH_KEY secret
cat ~/.ssh/voiceagent_deploy
```

---

## Auto-Deployment

| Trigger | What happens |
|---|---|
| Push to `main` in backend repo (changes under `src/**`) | .NET publish → SCP to VPS → `systemctl restart voiceagent-api` |
| Push to `main` in frontend repo | `npm run build` → SCP dist to VPS → `nginx reload` |
| `workflow_dispatch` button | Same as above, triggered manually from GitHub Actions UI |

EF Core migrations run **automatically on startup** — no manual `dotnet ef` needed.

---

## Manual Deployment

**Backend** (run locally or on CI):

```bash
dotnet publish src/VoiceAgent.Api \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o ./publish/api

scp -r ./publish/api/* deployuser@your-vps-ip:/var/www/voiceagent/api/
ssh deployuser@your-vps-ip "sudo systemctl restart voiceagent-api"
```

**Frontend:**

```bash
npm run build
scp -r ./dist/* deployuser@your-vps-ip:/var/www/voiceagent/frontend/
ssh deployuser@your-vps-ip "sudo nginx -t && sudo systemctl reload nginx"
```

---

## Verify Deployment

```bash
# Health check
curl https://yourdomain.com/api/health

# Swagger UI
open https://yourdomain.com/swagger

# Service status
sudo systemctl status voiceagent-api

# Live logs
sudo journalctl -u voiceagent-api -f

# Application logs
sudo tail -f /var/log/voiceagent/api-$(date +%Y%m%d).log
```

---

## Isolation from Existing Sites

| Resource | Existing sites | VoiceAgent |
|---|---|---|
| Web root | `/var/www/existing/` | `/var/www/voiceagent/` |
| Internal port | their ports | `5010` |
| PostgreSQL database | existing DB(s) | `voice_agent` |
| PostgreSQL user | existing user(s) | `va_user` |
| Nginx config | existing files in `sites-available/` | `sites-available/voiceagent` |
| systemd service | existing services | `voiceagent-api.service` |
| Logs | existing log dirs | `/var/log/voiceagent/` |
| Secrets file | — | `/etc/voiceagent/api.env` |

---

## Updating Secrets

To change an API key or DB password:

```bash
sudo nano /etc/voiceagent/api.env
# Edit the relevant line, save
sudo systemctl restart voiceagent-api
```

No redeployment needed — the service picks up the new values on restart.

---

## Rollback

GitHub Actions keeps every published artifact. To roll back:

1. Go to **Actions** → find the last good workflow run
2. Click **Re-run jobs** on that run

Or manually: SSH into VPS, replace `/var/www/voiceagent/api/` with a previous build, then `sudo systemctl restart voiceagent-api`.
