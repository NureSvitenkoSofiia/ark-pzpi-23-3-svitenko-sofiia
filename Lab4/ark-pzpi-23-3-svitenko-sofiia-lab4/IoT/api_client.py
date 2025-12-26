"""
API Client для комунікації з Backend сервером
"""

import urequests as requests
import ujson as json
from config import API_BASE_URL, PRINTER_ID


class ApiClient:
    """Клієнт для взаємодії з Backend API"""
    
    def __init__(self, base_url=API_BASE_URL, printer_id=PRINTER_ID):
        self.base_url = base_url.rstrip('/')
        self.printer_id = printer_id
    
    def _make_request(self, method, endpoint, data=None, timeout=10):
        """
        Виконує HTTP запит до API
        
        Args:
            method: HTTP метод (GET, POST)
            endpoint: API endpoint
            data: Дані для відправки (dict)
            timeout: Таймаут запиту
            
        Returns:
            dict: Відповідь від API або None при помилці
        """
        url = "{}{}".format(self.base_url, endpoint)
        headers = {'Content-Type': 'application/json'}
        
        try:
            if method == "GET":
                response = requests.get(url, timeout=timeout)
            elif method == "POST":
                json_data = json.dumps(data) if data else None
                response = requests.post(url, data=json_data, headers=headers, timeout=timeout)
            else:
                print("[API] Unsupported method: {}".format(method))
                return None
            
            if response.status_code in [200, 201]:
                # Деякі endpoints повертають пустий body (наприклад ping)
                if len(response.content) == 0:
                    response.close()
                    return {} 
                
                try:
                    result = response.json()
                    response.close()
                    return result
                except Exception as json_error:
                    print("[API] JSON parse error: {}".format(json_error))
                    print("[API] Response text: {}".format(response.text[:200] if response.text else "(empty)"))
                    response.close()
                    return None
            else:
                print("[API] Error {}: {}".format(response.status_code, response.text[:200]))
                response.close()
                return None
                
        except Exception as e:
            print("[API] Request failed: {}".format(e))
            return None
    
    def ping(self, status):
        """
        Відправити ping статус принтера
        
        Args:
            status: Статус принтера (idle/printing/error)
            
        Returns:
            bool: True якщо успішно
        """
        endpoint = "/api/Printer/{}/ping".format(self.printer_id)
        data = {"Status": status}
        
        print("[API] Ping: {}".format(status))
        result = self._make_request("POST", endpoint, data)
        return result is not None
    
    def get_queue(self):
        """
        Отримати чергу завдань для принтера
        
        Returns:
            list: Список завдань або порожній список
        """
        endpoint = "/api/Printer/{}/queue".format(self.printer_id)
        
        print("[API] Checking queue...")
        result = self._make_request("GET", endpoint)
        
        if result:
            if isinstance(result, dict) and 'data' in result:
                data = result['data']
            elif isinstance(result, dict) and 'Data' in result:
                data = result['Data']
            else:
                data = result
            
            if isinstance(data, list):
                print("[API] Queue has {} job(s)".format(len(data)))
                return data
        
        return []
    
    def start_job(self, job_id, estimated_time_minutes):
        """
        Розпочати виконання завдання
        
        Args:
            job_id: ID завдання
            estimated_time_minutes: Розрахований час друку (хвилини)
            
        Returns:
            bool: True якщо успішно
        """
        endpoint = "/api/Printer/{}/jobs/{}/start".format(self.printer_id, job_id)
        data = {"ActualEstimatedTime": estimated_time_minutes}
        
        print("[API] Starting job {}, estimate: {:.2f} min".format(job_id, estimated_time_minutes))
        result = self._make_request("POST", endpoint, data)
        return result is not None
    
    def finish_job(self, job_id, is_success, actual_material_grams=None, error_message=None):
        """
        Завершити виконання завдання
        
        Args:
            job_id: ID завдання
            is_success: Чи успішно завершено
            actual_material_grams: Фактична витрата матеріалу (грами)
            error_message: Повідомлення про помилку (якщо є)
            
        Returns:
            bool: True якщо успішно
        """
        endpoint = "/api/Printer/{}/jobs/{}/finish".format(self.printer_id, job_id)
        data = {
            "IsSuccess": is_success,
            "ActualMaterialInGrams": actual_material_grams,
            "ErrorMessage": error_message
        }
        
        status = "SUCCESS" if is_success else "FAILED"
        print("[API] Finishing job {}: {}".format(job_id, status))
        result = self._make_request("POST", endpoint, data, timeout=15)
        return result is not None
    
    def download_file(self, file_path, dest_path):
        """
        Завантажити файл через socket streaming (для ESP32 з обмеженою пам'яттю)
        
        Args:
            file_path: Шлях до файлу на сервері
            dest_path: Шлях для збереження
            
        Returns:
            bool: True якщо успішно
        """
        import socket
        
        try:
            print("[API] Downloading file...")
            print("  - Server path: {}".format(file_path))
            print("  - Local path: {}".format(dest_path))
            
            try:
                import gc
                gc.collect()
                print("  - Free memory: {} bytes".format(gc.mem_free()))
            except:
                pass
            
            url_parts = self.base_url.replace("http://", "").replace("https://", "").split(":")
            host = url_parts[0]
            port = int(url_parts[1]) if len(url_parts) > 1 else 80
            
            path = "/api/Printer/files/download?path={}".format(file_path)
            request = "GET {} HTTP/1.1\r\nHost: {}\r\nConnection: close\r\n\r\n".format(path, host)
            
            print("  - Connecting to {}:{}...".format(host, port))
            
            sock = socket.socket()
            sock.settimeout(60)
            addr = socket.getaddrinfo(host, port)[0][-1]
            sock.connect(addr)
            sock.send(request.encode())
            
            headers_done = False
            status_code = None
            content_length = None
            
            while not headers_done:
                line = sock.readline().decode('utf-8').strip()
                if not line:
                    headers_done = True
                elif line.startswith("HTTP/"):
                    status_code = int(line.split()[1])
                elif line.lower().startswith("content-length:"):
                    content_length = int(line.split(":")[1].strip())
            
            if status_code != 200:
                print("[API] Failed: status {}".format(status_code))
                sock.close()
                return False
            
            print("  - File size: {} bytes".format(content_length or "unknown"))
            
            total_bytes = 0
            chunk_size = 2048 
            
            with open(dest_path, 'wb') as f:
                while True:
                    chunk = sock.read(chunk_size)
                    if not chunk:
                        break
                    
                    f.write(chunk)
                    total_bytes += len(chunk)
                    
                    if total_bytes % 10240 == 0:
                        print("  - Progress: {} bytes".format(total_bytes))
                    
                    if content_length and total_bytes >= content_length:
                        break
            
            sock.close()
            print("[API] Downloaded successfully ({} bytes)".format(total_bytes))
            
            try:
                import gc
                gc.collect()
            except:
                pass
            
            return True
            
        except Exception as e:
            print("[API] Download error: {}".format(e))
            try:
                sock.close()
            except:
                pass
            return False

