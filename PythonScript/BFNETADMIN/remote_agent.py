# -*- coding: utf-8 -*-
import os
import sys
import json
import socket
import threading

_DIR = os.path.dirname(os.path.abspath(__file__))
if _DIR not in sys.path:
    sys.path.insert(0, _DIR)

from command_registry import CommandRegistry

class RemoteAgentServer:
    def __init__(self, host: str = "0.0.0.0", port: int = 9999):
        self.host = host
        self.port = port
        self.server_socket = None
        self.is_running = False

    def start(self):
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.server_socket.bind((self.host, self.port))
        self.server_socket.listen(5)
        self.is_running = True
        print(f"[*] Agent Server listening on {self.host}:{self.port}")

        while self.is_running:
            try:
                client_sock, addr = self.server_socket.accept()
                threading.Thread(target=self._handle_client, args=(client_sock, addr), daemon=True).start()
            except Exception:
                break

    def _handle_client(self, client_sock: socket.socket, addr):
        try:
            data = client_sock.recv(4096).decode("utf-8")
            if not data:
                return
            payload = json.loads(data)
            res = CommandRegistry.dispatch_from_dict(payload)
            client_sock.sendall(json.dumps(res, ensure_ascii=False).encode("utf-8"))
        except Exception as e:
            err_res = {"status": "error", "data": None, "message": str(e)}
            client_sock.sendall(json.dumps(err_res).encode("utf-8"))
        finally:
            client_sock.close()

    def stop(self):
        self.is_running = False
        if self.server_socket:
            self.server_socket.close()

if __name__ == "__main__":
    import scanner
    import network_utils
    import ad_manager
    srv = RemoteAgentServer()
    srv.start()
