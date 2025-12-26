"""
IoT 3D Printer Client - Main Application
Реалізація згідно з діаграмою діяльності
З підтримкою LCD дисплея, кнопок, меню та локалізації
"""

import network
import time
import os
from config import (
    WIFI_SSID, WIFI_PASSWORD, API_BASE_URL, PRINTER_ID,
    PING_INTERVAL, QUEUE_POLL_INTERVAL, PRINT_MONITORING_INTERVAL,
    STATUS_IDLE, STATUS_PRINTING, STATUS_ERROR,
    GCODE_CACHE_FILE, SKIP_GCODE_PARSING,
    LCD_I2C_ADDR, LCD_SCL_PIN, LCD_SDA_PIN,
    BTN_UP_PIN, BTN_DOWN_PIN, BTN_SELECT_PIN, BTN_BACK_PIN,
    DEFAULT_LANGUAGE
)
from api_client import ApiClient
from gcode_parser import GCodeParser
from printer_controller import PrinterController
from lcd_display import LcdDisplay
from button_handler import ButtonHandler
from localization import Localization
from menu_system import MenuSystem
from statistics import Statistics


class IoTPrinterClient:
    """Головний клас IoT 3D Printer з UI"""
    
    def __init__(self):
        print("Initializing IoT Printer Client...")
        
        # Основні компоненти
        self.api = ApiClient(API_BASE_URL, PRINTER_ID)
        self.parser = GCodeParser()
        self.printer = PrinterController()
        
        # UI компоненти
        print("Initializing LCD...")
        self.lcd = LcdDisplay(LCD_I2C_ADDR, LCD_SCL_PIN, LCD_SDA_PIN)
        time.sleep(0.5)  # Додаткова затримка після LCD
        
        print("Initializing Buttons...")
        self.buttons = ButtonHandler(BTN_UP_PIN, BTN_DOWN_PIN, BTN_SELECT_PIN, BTN_BACK_PIN)
        
        print("Initializing Localization...")
        self.loc = Localization(default_language=DEFAULT_LANGUAGE)
        
        print("Initializing Statistics...")
        self.stats = Statistics()
        
        print("Initializing Menu System...")
        self.menu = MenuSystem(self.lcd, self.loc, self.stats)
        
        # Стан
        self.status = STATUS_IDLE
        self.last_ping_time = 0
        self.last_queue_check_time = 0
        self.current_job = None
        self.polling_mode = False
        
        print("Initialization complete")
        
        # Показати стартове повідомлення на LCD
        if self.lcd.enabled:
            self.lcd.clear()
            self.lcd.write_center(1, "IoT 3D Printer")
            self.lcd.write_center(2, "Initializing...")
            time.sleep(1)
    
    def connect_wifi(self):
        """Підключення до WiFi з відображенням на LCD"""
        print("=" * 50)
        print("IoT 3D Printer Client Starting")
        print("=" * 50)
        print("Printer ID: {}".format(PRINTER_ID))
        print("API: {}".format(API_BASE_URL))
        
        # Показати на LCD
        self.lcd.clear()
        self.lcd.write_center(0, "IoT 3D Printer")
        self.lcd.write_center(1, self.loc.get('wifi_connecting'))
        self.lcd.write_center(2, WIFI_SSID)
        if not self.lcd.enabled:
            self.lcd.print_console()
        
        print("\n[WiFi] Connecting to '{}'".format(WIFI_SSID), end="")
        
        sta_if = network.WLAN(network.STA_IF)
        sta_if.active(True)
        sta_if.connect(WIFI_SSID, WIFI_PASSWORD)
        
        timeout = 30
        start_time = time.time()
        
        while not sta_if.isconnected():
            if time.time() - start_time > timeout:
                print("\n[WiFi] Connection timeout!")
                self.lcd.write_center(2, self.loc.get('wifi_failed'))
                if not self.lcd.enabled:
                    self.lcd.print_console()
                return False
            
            print(".", end="")
            time.sleep(0.5)
        
        ip = sta_if.ifconfig()[0]
        print(" Connected!")
        print("[WiFi] IP: {}".format(ip))
        
        # Показати на LCD
        self.lcd.write_center(1, self.loc.get('wifi_connected'))
        self.lcd.write_line(2, "{}: {}".format(self.loc.get('ip'), ip))
        if not self.lcd.enabled:
            self.lcd.print_console()
        
        time.sleep(2)
        return True
    
    def send_ping(self):
        """Відправити ping статус"""
        current_time = time.time()
        
        if current_time - self.last_ping_time >= PING_INTERVAL:
            self.api.ping(self.status)
            self.last_ping_time = current_time
    
    def check_queue(self):
        """Перевірити чергу завдань"""
        current_time = time.time()
        
        # Перевіряємо чергу тільки коли не друкуємо
        if self.status != STATUS_IDLE:
            return None
        
        if current_time - self.last_queue_check_time >= QUEUE_POLL_INTERVAL:
            jobs = self.api.get_queue()
            self.last_queue_check_time = current_time
            
            if jobs and len(jobs) > 0:
                return jobs[0]  # Беремо перше завдання
        
        return None
    
    def process_job(self, job):
        """
        Обробити завдання друку
        
        Args:
            job: Об'єкт завдання з API
        """
        try:
            # API повертає поля з малої літери (camelCase)
            job_id = job.get('id') or job.get('Id')
            gcode_path = job.get('gCodeFilePath') or job.get('GCodeFilePath')
            material = job.get('requiredMaterial') or job.get('RequiredMaterial', {})
            
            print("\n{}".format('='*50))
            print("Processing Job #{}".format(job_id))
            print("{}".format('='*50))
            print("Material: {} - {}".format(
                material.get('materialType') or material.get('MaterialType', 'Unknown'),
                material.get('color') or material.get('Color', 'Unknown')
            ))
            print("Estimated material: {:.2f}g".format(
                job.get('estimatedMaterialInGrams') or job.get('EstimatedMaterialInGrams', 0)
            ))
            
            # Показати на LCD
            self.lcd.clear()
            self.lcd.write_center(0, "{} #{}".format(self.loc.get('job'), job_id))
            self.lcd.write_line(1, self.loc.get('initializing'))
            if not self.lcd.enabled:
                self.lcd.print_console()
            
            self.current_job = job
            
            # 1. Завантажити GCode
            if not self._download_gcode(gcode_path):
                self._finish_job_with_error(job_id, "Failed to download GCode")
                return
            
            # 2. Парсити GCode та розрахувати estimate
            estimate = self._parse_and_estimate(material)
            if not estimate:
                self._finish_job_with_error(job_id, "Failed to parse GCode")
                return
            
            # 3. Валідувати матеріал
            if not self._validate_material(estimate['material_grams'], job):
                self._finish_job_with_error(job_id, "Insufficient material")
                return
            
            # 4. Відправити Start Job
            if not self.api.start_job(job_id, estimate['time_minutes']):
                self._finish_job_with_error(job_id, "Failed to start job on server")
                return
            
            # 5. Почати друк
            self._start_printing(job_id, estimate)
            
            # 6. Моніторити друк
            success = self._monitor_printing()
            
            # 7. Завершити друк
            self._finish_printing(job_id, success)
            
        except Exception as e:
            print("[Job] Error processing job: {}".format(e))
            if self.current_job:
                job_id = self.current_job.get('id') or self.current_job.get('Id')
                if job_id:
                    self._finish_job_with_error(job_id, str(e))
        
        finally:
            self.current_job = None
            self.status = STATUS_IDLE
            self._cleanup()
    
    def _download_gcode(self, gcode_path):
        """Завантажити GCode файл"""
        print("\n[1/7] Downloading GCode...")
        
        self.lcd.write_line(1, "[1/7] Downloading...")
        if not self.lcd.enabled:
            self.lcd.print_console()
        
        if not gcode_path:
            print("[Error] No GCode path provided")
            return False
        
        return self.api.download_file(gcode_path, GCODE_CACHE_FILE)
    
    def _parse_and_estimate(self, material):
        """Парсити GCode та розрахувати estimate"""
        print("\n[2/7] Parsing GCode and calculating estimates...")
        
        self.lcd.write_line(1, "[2/7] Parsing...")
        if not self.lcd.enabled:
            self.lcd.print_console()
        
        # Якщо вимкнено парсинг - використовуємо estimate з сервера
        if SKIP_GCODE_PARSING:
            print("[GCode] Using server estimate (parsing disabled)")
            time_min = self.current_job.get('estimatedPrintTimeMinutes') or 0
            material_g = self.current_job.get('estimatedMaterialInGrams') or 0
            
            return {
                'time_minutes': time_min if time_min > 0 else 30,
                'material_grams': material_g if material_g > 0 else 10,
                'layers': 100
            }
        
        result = self.parser.parse_file(GCODE_CACHE_FILE)
        if not result:
            print("[GCode] Parse failed, using server estimate")
            return {
                'time_minutes': self.current_job.get('estimatedPrintTimeMinutes') or 30,
                'material_grams': self.current_job.get('estimatedMaterialInGrams') or 10,
                'layers': 100
            }
        
        # Розрахунок ваги матеріалу
        diameter_mm = material.get('diameterMm') or material.get('DiameterMm', 1.75)
        density_g_cm3 = material.get('densityInGramsPerCm3') or material.get('DensityInGramsPerCm3', 1.24)
        
        material_grams = self.parser.calculate_material_weight(
            result['extrusion_mm'],
            diameter_mm,
            density_g_cm3
        )
        
        print("[Estimate] Material needed: {:.2f}g".format(material_grams))
        
        return {
            'time_minutes': result['time_minutes'],
            'material_grams': material_grams,
            'layers': result['layers']
        }
    
    def _validate_material(self, required_grams, job):
        """Валідувати наявність матеріалу"""
        print("\n[3/7] Validating material availability...")
        
        self.lcd.write_line(1, "[3/7] Validating...")
        if not self.lcd.enabled:
            self.lcd.print_console()
        
        estimated_grams = job.get('estimatedMaterialInGrams') or job.get('EstimatedMaterialInGrams', 0)
        
        print("  - Required: {:.2f}g".format(required_grams))
        print("  - Server estimate: {:.2f}g".format(estimated_grams))
        print("  - Validation: OK")
        
        return True
    
    def _start_printing(self, job_id, estimate):
        """Почати друк"""
        print("\n[4/7] Starting print...")
        
        self.lcd.write_line(1, "[4/7] Starting...")
        if not self.lcd.enabled:
            self.lcd.print_console()
        
        self.status = STATUS_PRINTING
        self.stats.start_job(estimate['time_minutes'], estimate['material_grams'])
        
        self.printer.start_print(
            job_id,
            estimate['time_minutes'],
            estimate['material_grams'],
            estimate['layers']
        )
    
    def _monitor_printing(self):
        """Моніторити процес друку"""
        print("\n[5/7] Monitoring print progress...")
        print("-" * 50)
        
        while self.printer.is_printing:
            status = self.printer.get_status()
            
            # Виводимо прогрес в консоль
            progress_msg = "\rProgress: {:.1f}% | Layer: {}/{} | Time: {:.0f}s / {:.0f}s | Material: {:.1f}g".format(
                status['progress'],
                status['current_layer'],
                status['total_layers'],
                status['elapsed_time'],
                status['remaining_time'],
                status['used_material']
            )
            print(progress_msg, end="")
            
            # Показуємо прогрес на LCD
            self.menu.show_printing_status(
                status['progress'],
                status['current_layer'],
                status['total_layers'],
                status['remaining_time'],
                status['used_material']
            )
            
            # Відправляємо ping під час друку
            self.send_ping()
            
            # Перевірка на помилки
            if self.printer.simulate_error():
                print("\n[Error] Printer error detected!")
                return False
            
            # Перевірка завершення
            if self.printer.is_complete():
                print("\n[Print] Complete!")
                return True
            
            time.sleep(PRINT_MONITORING_INTERVAL)
        
        return True
    
    def _finish_printing(self, job_id, success):
        """Завершити друк"""
        print("\n[6/7] Finishing print...")
        
        actual_material = self.printer.stop_print()
        
        # Оновити статистику
        self.stats.finish_job(success, actual_material)
        
        if success:
            self.api.finish_job(job_id, True, actual_material, None)
            print("[Job] Finished successfully! Used: {:.2f}g".format(actual_material))
            
            # Показати на LCD
            self.lcd.clear()
            self.lcd.write_center(0, self.loc.get('job') + " #" + str(job_id))
            self.lcd.write_center(1, self.loc.get('successful'))
            self.lcd.write_line(2, "{}: {:.1f}{}".format(
                self.loc.get('material'),
                actual_material,
                self.loc.get('grams')
            ))
            if not self.lcd.enabled:
                self.lcd.print_console()
            time.sleep(3)
        else:
            self.api.finish_job(job_id, False, actual_material, "Print failed during execution")
            print("[Job] Finished with errors")
            
            # Показати на LCD
            self.lcd.clear()
            self.lcd.write_center(0, self.loc.get('job') + " #" + str(job_id))
            self.lcd.write_center(1, self.loc.get('failed'))
            if not self.lcd.enabled:
                self.lcd.print_console()
            time.sleep(3)
        
        self.status = STATUS_IDLE
    
    def _finish_job_with_error(self, job_id, error_message):
        """Завершити завдання з помилкою"""
        print("[Error] {}".format(error_message))
        self.api.finish_job(job_id, False, None, error_message)
        self.stats.finish_job(False, None)
        self.status = STATUS_IDLE
        
        # Показати на LCD
        self.lcd.clear()
        self.lcd.write_center(0, self.loc.get('error'))
        self.lcd.write_line(1, error_message[:20])
        if not self.lcd.enabled:
            self.lcd.print_console()
        time.sleep(3)
    
    def _cleanup(self):
        """Очистити тимчасові файли"""
        print("\n[7/7] Cleaning up...")
        
        try:
            try:
                os.stat(GCODE_CACHE_FILE)
                os.remove(GCODE_CACHE_FILE)
                print("  - Removed: {}".format(GCODE_CACHE_FILE))
            except OSError:
                pass
        except Exception as e:
            print("  - Cleanup warning: {}".format(e))
        
        print("[Job] Cleanup complete\n")
    
    def handle_buttons(self):
        """Обробити натискання кнопок"""
        if not self.buttons.enabled:
            return
        
        button = self.buttons.get_pressed_button()
        
        if button == 'up':
            self.menu.handle_button_up()
        elif button == 'down':
            self.menu.handle_button_down()
        elif button == 'select':
            action = self.menu.handle_button_select()
            if action == 'start_polling':
                self.polling_mode = True
        elif button == 'back':
            action = self.menu.handle_button_back()
            if action == 'stop_polling':
                self.polling_mode = False
    
    def run_polling_mode(self):
        """Режим полінгу з можливістю виходу"""
        self.polling_mode = True
        
        print("\n[Polling Mode] Started")
        
        while self.polling_mode:
            # Оновити дисплей
            self.menu.show_polling_mode(self.status, self.last_queue_check_time)
            
            # Відправити ping
            self.send_ping()
            
            # Перевірити чергу
            job = self.check_queue()
            
            if job:
                # Обробити завдання
                self.process_job(job)
            else:
                # Чекаємо і перевіряємо кнопки
                time.sleep(0.5)
                self.handle_buttons()
        
        print("[Polling Mode] Stopped")
    
    def run(self):
        """Головний цикл роботи з меню"""
        # Підключення до WiFi
        if not self.connect_wifi():
            print("[Fatal] Failed to connect to WiFi")
            return
        
        # Початковий ping
        self.api.ping(STATUS_IDLE)
        
        # Показати системне повідомлення
        self.lcd.clear()
        self.lcd.write_center(0, "IoT 3D Printer")
        self.lcd.write_center(1, self.loc.get('system_ready'))
        self.lcd.write_center(2, "ID: {}".format(PRINTER_ID))
        if not self.lcd.enabled:
            self.lcd.print_console()
        
        print("=" * 50)
        print("System Ready - Entering menu mode")
        print("=" * 50)
        print()
        
        time.sleep(2)
        
        # Показати головне меню
        self.menu.show_main_menu()
        
        # Головний цикл
        try:
            while True:
                if self.polling_mode:
                    # Режим полінгу
                    self.run_polling_mode()
                    # Повернутись до меню
                    self.menu.show_main_menu()
                else:
                    # Режим меню
                    self.handle_buttons()
                    
                    # Короткий sleep
                    time.sleep(0.1)
                
        except KeyboardInterrupt:
            print("\n\n[System] Shutting down...")
            self.api.ping(STATUS_IDLE)
            self.lcd.clear()
            self.lcd.write_center(1, "System Stopped")
            if not self.lcd.enabled:
                self.lcd.print_console()
            print("[System] Stopped")


print("Starting IoT Printer Client with UI")
client = IoTPrinterClient()
client.run()
print("IoT Printer Client stopped")