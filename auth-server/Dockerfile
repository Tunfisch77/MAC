FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt && groupadd --gid 10001 mac && useradd --system --uid 10001 --gid 10001 mac && mkdir /data && chown mac /data
COPY server.py manage.py gunicorn.conf.py entrypoint.py ./
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1 MAC_DATABASE=/data/mac-auth.sqlite3
EXPOSE 10000
CMD ["python", "entrypoint.py"]
