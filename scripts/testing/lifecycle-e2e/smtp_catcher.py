import socket
import threading
import email
from email.header import decode_header
import re
import json
import os
import time


class LocalSmtpCatcher:
    """Lightweight ESMTP server on 127.0.0.1 for capturing OTPs, invitation links, and notifications."""

    def __init__(self, host="127.0.0.1", port=2525, storage_path=None):
        self.host = host
        self.port = port
        self.storage_path = storage_path
        self.messages = []
        self._lock = threading.Lock()
        self._running = False
        self._server_socket = None
        self._thread = None

    def start(self):
        self._server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._server_socket.bind((self.host, self.port))
        self._server_socket.listen(20)
        self._server_socket.settimeout(1.0)
        self._running = True
        self._thread = threading.Thread(target=self._accept_loop, daemon=True)
        self._thread.start()

    def stop(self):
        self._running = False
        if self._server_socket:
            try:
                self._server_socket.close()
            except Exception:
                pass
        if self._thread:
            self._thread.join(timeout=3.0)

    def _accept_loop(self):
        while self._running:
            try:
                conn, addr = self._server_socket.accept()
                t = threading.Thread(target=self._handle_client, args=(conn, addr), daemon=True)
                t.start()
            except socket.timeout:
                continue
            except Exception:
                break

    def _handle_client(self, conn: socket.socket, addr):
        conn.settimeout(10.0)
        try:
            conn.sendall(b"220 localhost ESMTP ProcurementTestCatcher Ready\r\n")
            mail_from = ""
            rcpt_to = []
            in_data = False
            data_buffer = bytearray()
            auth_step = None

            file_obj = conn.makefile("rb")
            while True:
                line = file_obj.readline()
                if not line:
                    break

                if in_data:
                    if line in (b".\r\n", b".\n"):
                        in_data = False
                        self._store_message(mail_from, list(rcpt_to), bytes(data_buffer))
                        data_buffer.clear()
                        conn.sendall(b"250 2.0.0 OK: Message accepted\r\n")
                    else:
                        if line.startswith(b".."):
                            line = line[1:]
                        data_buffer.extend(line)
                    continue

                cmd_line = line.decode("utf-8", errors="replace").strip()
                upper = cmd_line.upper()

                if auth_step == "WAIT_USER":
                    auth_step = "WAIT_PASS"
                    conn.sendall(b"334 UGFzc3dvcmQ6\r\n")
                    continue
                elif auth_step == "WAIT_PASS":
                    auth_step = None
                    conn.sendall(b"235 2.7.0 Authentication successful\r\n")
                    continue

                if upper.startswith("EHLO") or upper.startswith("HELO"):
                    conn.sendall(
                        b"250-localhost Hello\r\n"
                        b"250-SIZE 35882577\r\n"
                        b"250-AUTH LOGIN PLAIN\r\n"
                        b"250 OK\r\n"
                    )
                elif upper.startswith("AUTH LOGIN"):
                    parts = cmd_line.split()
                    if len(parts) > 2:
                        auth_step = "WAIT_PASS"
                        conn.sendall(b"334 UGFzc3dvcmQ6\r\n")
                    else:
                        auth_step = "WAIT_USER"
                        conn.sendall(b"334 VXNlcm5hbWU6\r\n")
                elif upper.startswith("AUTH PLAIN"):
                    conn.sendall(b"235 2.7.0 Authentication successful\r\n")
                elif upper.startswith("MAIL FROM:"):
                    mail_from = cmd_line[10:].strip()
                    rcpt_to = []
                    conn.sendall(b"250 2.1.0 OK\r\n")
                elif upper.startswith("RCPT TO:"):
                    rcpt_to.append(cmd_line[8:].strip().strip("<>"))
                    conn.sendall(b"250 2.1.5 OK\r\n")
                elif upper == "DATA":
                    in_data = True
                    data_buffer.clear()
                    conn.sendall(b"354 End data with <CR><LF>.<CR><LF>\r\n")
                elif upper.startswith("RSET"):
                    mail_from = ""
                    rcpt_to = []
                    in_data = False
                    data_buffer.clear()
                    conn.sendall(b"250 2.0.0 OK\r\n")
                elif upper.startswith("NOOP"):
                    conn.sendall(b"250 2.0.0 OK\r\n")
                elif upper.startswith("QUIT"):
                    conn.sendall(b"221 2.0.0 Bye\r\n")
                    break
                else:
                    conn.sendall(b"250 OK\r\n")
        except Exception:
            pass
        finally:
            try:
                conn.close()
            except Exception:
                pass

    def _decode_mime_header(self, value):
        if not value:
            return ""
        parts = decode_header(value)
        decoded = []
        for content, enc in parts:
            if isinstance(content, bytes):
                decoded.append(content.decode(enc or "utf-8", errors="replace"))
            else:
                decoded.append(str(content))
        return "".join(decoded)

    def _extract_body(self, msg):
        body_parts = []
        if msg.is_multipart():
            for part in msg.walk():
                ctype = part.get_content_type()
                if ctype in ("text/plain", "text/html"):
                    payload = part.get_payload(decode=True)
                    if payload:
                        charset = part.get_content_charset() or "utf-8"
                        body_parts.append(payload.decode(charset, errors="replace"))
        else:
            payload = msg.get_payload(decode=True)
            if payload:
                charset = msg.get_content_charset() or "utf-8"
                body_parts.append(payload.decode(charset, errors="replace"))
        return "\n".join(body_parts)

    def _store_message(self, mail_from, rcpt_to, raw_bytes):
        msg = email.message_from_bytes(raw_bytes)
        subject = self._decode_mime_header(msg.get("Subject", ""))
        body = self._extract_body(msg)

        otp_match = re.search(r"(?<!#)\b(\d{6})\b", body)
        inv_match = re.search(r"token=([A-Za-z0-9_\-]+)", body)
        pwd_match = re.search(r"<code>([^<]+)</code>", body)

        record = {
            "timestampUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "from": mail_from,
            "to": rcpt_to,
            "subject": subject,
            "body": body,
            "extractedOtp": otp_match.group(1) if otp_match else None,
            "extractedInviteToken": inv_match.group(1) if inv_match else None,
            "extractedTempPassword": pwd_match.group(1).strip() if pwd_match else None,
        }
        with self._lock:
            self.messages.append(record)
            if self.storage_path:
                try:
                    with open(self.storage_path, "w", encoding="utf-8") as f:
                        json.dump(self.messages, f, indent=2, ensure_ascii=False)
                except Exception:
                    pass

    def wait_for_email(self, recipient_email: str, timeout_sec: float = 10.0, min_index: int = 0):
        target = recipient_email.lower().strip()
        deadline = time.time() + timeout_sec
        while time.time() < deadline:
            with self._lock:
                for idx in range(len(self.messages) - 1, min_index - 1, -1):
                    m = self.messages[idx]
                    recipients = [r.lower() for r in m["to"]]
                    if any(target in r for r in recipients):
                        return m
            time.sleep(0.25)
        return None

    def count_messages(self):
        with self._lock:
            return len(self.messages)
