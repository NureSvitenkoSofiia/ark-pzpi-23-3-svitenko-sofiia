"""
Керування LCD дисплеєм для IoT 3D Printer
Підтримує I2C LCD 20x4
"""

import time

try:
    from machine import Pin, I2C
    from lcd_api import LcdApi
    from i2c_lcd import I2cLcd
    HAS_LCD = True
except ImportError:
    HAS_LCD = False
    print("[LCD] Hardware not available - using console output")


class LcdDisplay:
    """Керування LCD дисплеєм"""
    
    # Розміри LCD
    LCD_ROWS = 4
    LCD_COLS = 20
    
    def __init__(self, i2c_addr=0x27, scl_pin=22, sda_pin=21):
        """
        Ініціалізація LCD дисплея
        
        Args:
            i2c_addr: I2C адреса LCD
            scl_pin: Pin для SCL
            sda_pin: Pin для SDA
        """
        self.enabled = False
        self.lcd = None
        
        if HAS_LCD:
            try:
                i2c = I2C(0, scl=Pin(scl_pin), sda=Pin(sda_pin), freq=400000)
                time.sleep(0.1)  # Затримка для стабілізації I2C
                
                # Сканування I2C пристроїв
                devices = i2c.scan()
                print("[LCD] I2C devices found: {}".format([hex(d) for d in devices]))
                
                # Спробувати підключитися до LCD
                if i2c_addr not in devices:
                    print("[LCD] Warning: Address 0x{:02X} not found!".format(i2c_addr))
                    # Спробувати альтернативну адресу
                    if 0x3F in devices and i2c_addr == 0x27:
                        print("[LCD] Trying alternative address 0x3F...")
                        i2c_addr = 0x3F
                    elif 0x27 in devices and i2c_addr == 0x3F:
                        print("[LCD] Trying alternative address 0x27...")
                        i2c_addr = 0x27
                
                self.lcd = I2cLcd(i2c, i2c_addr, self.LCD_ROWS, self.LCD_COLS)
                time.sleep(0.5)  # Затримка після ініціалізації LCD
                
                self.lcd.backlight_on()  # Увімкнути підсвітку
                time.sleep(0.1)
                
                self.lcd.clear()  # Очистити екран
                time.sleep(0.2)
                
                self.enabled = True
                print("[LCD] Initialized {}x{} at I2C 0x{:02X}".format(self.LCD_COLS, self.LCD_ROWS, i2c_addr))
                
                # Тестовий вивід
                self.lcd.move_to(0, 0)
                self.lcd.putstr("IoT 3D Printer")
                time.sleep(0.1)
                self.lcd.move_to(0, 1)
                self.lcd.putstr("LCD Initialized!")
                time.sleep(1)
            except Exception as e:
                print("[LCD] Failed to initialize: {}".format(e))
                import sys
                sys.print_exception(e)
                self.enabled = False
        
        # Буфер для консолі (якщо LCD недоступний)
        self.console_buffer = [''] * self.LCD_ROWS
    
    def clear(self):
        """Очистити екран"""
        if self.enabled and self.lcd:
            self.lcd.clear()
        self.console_buffer = [''] * self.LCD_ROWS
    
    def write_line(self, row, text, col=0):
        """
        Написати текст на вказаний рядок
        
        Args:
            row: Номер рядка (0-3)
            text: Текст для виводу
            col: Стовпець початку (за замовчуванням 0)
        """
        if row >= self.LCD_ROWS:
            return
        
        # Обрізати текст до розміру LCD
        text = str(text)[:self.LCD_COLS - col]
        
        if self.enabled and self.lcd:
            try:
                self.lcd.move_to(col, row)
                # Очистити рядок пробілами
                self.lcd.putstr(' ' * (self.LCD_COLS - col))
                self.lcd.move_to(col, row)
                self.lcd.putstr(text)
            except Exception as e:
                print("[LCD] Error writing line {}: {}".format(row, e))
        
        # Зберегти в буфер для консолі
        self.console_buffer[row] = text
    
    def write_center(self, row, text):
        """
        Написати текст по центру рядка
        
        Args:
            row: Номер рядка
            text: Текст для виводу
        """
        text = str(text)
        if len(text) > self.LCD_COLS:
            text = text[:self.LCD_COLS]
        
        col = (self.LCD_COLS - len(text)) // 2
        self.write_line(row, text, col)
    
    def write_multiline(self, lines):
        """
        Написати декілька рядків
        
        Args:
            lines: Список текстів (до 4 рядків)
        """
        for i, line in enumerate(lines[:self.LCD_ROWS]):
            self.write_line(i, line)
    
    def show_menu(self, title, items, selected_index):
        """
        Показати меню
        
        Args:
            title: Заголовок меню
            items: Список пунктів меню
            selected_index: Індекс вибраного пункту
        """
        print("[LCD] show_menu called - enabled: {}, title: {}".format(self.enabled, title))
        self.clear()
        self.write_center(0, title)
        
        # Показуємо до 3 пунктів меню (рядки 1-3)
        visible_items = 3
        start_index = max(0, selected_index - visible_items + 1)
        
        for i in range(visible_items):
            item_index = start_index + i
            if item_index < len(items):
                prefix = '>' if item_index == selected_index else ' '
                text = "{} {}".format(prefix, items[item_index])
                self.write_line(i + 1, text)
        
        print("[LCD] Menu displayed: {} items".format(len(items)))
    
    def show_status(self, status_line1, status_line2=None, status_line3=None, status_line4=None):
        """
        Показати статус
        
        Args:
            status_line1-4: Рядки статусу
        """
        lines = [status_line1]
        if status_line2:
            lines.append(status_line2)
        if status_line3:
            lines.append(status_line3)
        if status_line4:
            lines.append(status_line4)
        
        self.write_multiline(lines)
    
    def show_progress(self, title, progress, details=None):
        """
        Показати прогрес
        
        Args:
            title: Заголовок
            progress: Прогрес (0-100)
            details: Додаткові деталі (словник)
        """
        self.clear()
        self.write_line(0, title)
        
        # Прогрес бар
        bar_length = self.LCD_COLS - 6  # Місце для "[", "]", та "100%"
        filled = int(bar_length * progress / 100)
        bar = '[' + '#' * filled + '-' * (bar_length - filled) + ']'
        self.write_line(1, bar)
        self.write_line(1, "{:>3.0f}%".format(progress), self.LCD_COLS - 4)
        
        # Деталі
        if details:
            row = 2
            for key, value in details.items():
                if row >= self.LCD_ROWS:
                    break
                text = "{}: {}".format(key, value)
                self.write_line(row, text)
                row += 1
    
    def print_console(self):
        """Вивести буфер в консоль (для відлагодження)"""
        if not self.enabled:
            print("\n" + "=" * self.LCD_COLS)
            for i, line in enumerate(self.console_buffer):
                print("|{:<{}}|".format(line, self.LCD_COLS))
            print("=" * self.LCD_COLS)

