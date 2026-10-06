# -*- coding: utf-8 -*-
import os
import sqlite3
import threading
from datetime import datetime

_DB_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "bfnetadmin_audit.db")
_lock = threading.Lock()

def init_db():
    with _lock:
        with sqlite3.connect(_DB_FILE) as conn:
            conn.execute("""
                CREATE TABLE IF NOT EXISTS audit_logs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp TEXT,
                    source TEXT,
                    action TEXT,
                    status TEXT,
                    details TEXT
                )
            """)
            conn.commit()

def log_event(action: str, status: str, details: str = "", source: str = "LOCAL"):
    try:
        init_db()
        with _lock:
            with sqlite3.connect(_DB_FILE) as conn:
                conn.execute(
                    "INSERT INTO audit_logs (timestamp, source, action, status, details) VALUES (?, ?, ?, ?, ?)",
                    (datetime.now().isoformat(), source, action, status, str(details))
                )
                conn.commit()
    except Exception:
        pass
