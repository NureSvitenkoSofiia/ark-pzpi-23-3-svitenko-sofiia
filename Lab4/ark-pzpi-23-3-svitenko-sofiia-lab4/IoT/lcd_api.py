"""
LCD API для MicroPython
Базовий клас для роботи з LCD дисплеями
"""

import time


class LcdApi:
    """Базовий API для LCD дисплеїв"""
    
    # Команди
    LCD_CLR = 0x01              # Очистити дисплей
    LCD_HOME = 0x02             # Повернутись додому
    LCD_ENTRY_MODE = 0x04       # Режим вводу
    LCD_DISPLAY_CTRL = 0x08     # Керування дисплеєм
    LCD_FUNCTION = 0x20         # Функції
    LCD_DDRAM = 0x80            # DDRAM адреса
    
    # Прапорці для режиму вводу
    LCD_ENTRY_RIGHT = 0x00
    LCD_ENTRY_LEFT = 0x02
    LCD_ENTRY_SHIFT_INCREMENT = 0x01
    LCD_ENTRY_SHIFT_DECREMENT = 0x00
    
    # Прапорці для керування дисплеєм
    LCD_DISPLAY_ON = 0x04
    LCD_DISPLAY_OFF = 0x00
    LCD_CURSOR_ON = 0x02
    LCD_CURSOR_OFF = 0x00
    LCD_BLINK_ON = 0x01
    LCD_BLINK_OFF = 0x00
    
    # Прапорці для функцій
    LCD_8BITMODE = 0x10
    LCD_4BITMODE = 0x00
    LCD_2LINE = 0x08
    LCD_1LINE = 0x00
    LCD_5x10DOTS = 0x04
    LCD_5x8DOTS = 0x00
    
    def __init__(self, num_lines, num_columns):
        """Ініціалізація LCD API"""
        self.num_lines = num_lines
        self.num_columns = num_columns
        self.cursor_x = 0
        self.cursor_y = 0
        self.backlight = True
        self.display_ctrl = self.LCD_DISPLAY_ON | self.LCD_CURSOR_OFF | self.LCD_BLINK_OFF
        self.display_mode = self.LCD_ENTRY_LEFT | self.LCD_ENTRY_SHIFT_DECREMENT
        self.hal_write_command(self.LCD_ENTRY_MODE | self.display_mode)
    
    def clear(self):
        """Очистити екран"""
        self.hal_write_command(self.LCD_CLR)
        self.hal_write_command(self.LCD_HOME)
        self.cursor_x = 0
        self.cursor_y = 0
        time.sleep_ms(2)
    
    def show_cursor(self):
        """Показати курсор"""
        self.display_ctrl |= self.LCD_CURSOR_ON
        self.hal_write_command(self.LCD_DISPLAY_CTRL | self.display_ctrl)
    
    def hide_cursor(self):
        """Сховати курсор"""
        self.display_ctrl &= ~self.LCD_CURSOR_ON
        self.hal_write_command(self.LCD_DISPLAY_CTRL | self.display_ctrl)
    
    def blink_cursor_on(self):
        """Увімкнути миготіння курсора"""
        self.display_ctrl |= self.LCD_BLINK_ON
        self.hal_write_command(self.LCD_DISPLAY_CTRL | self.display_ctrl)
    
    def blink_cursor_off(self):
        """Вимкнути миготіння курсора"""
        self.display_ctrl &= ~self.LCD_BLINK_ON
        self.hal_write_command(self.LCD_DISPLAY_CTRL | self.display_ctrl)
    
    def display_on(self):
        """Увімкнути дисплей"""
        self.display_ctrl |= self.LCD_DISPLAY_ON
        self.hal_write_command(self.LCD_DISPLAY_CTRL | self.display_ctrl)
    
    def display_off(self):
        """Вимкнути дисплей"""
        self.display_ctrl &= ~self.LCD_DISPLAY_ON
        self.hal_write_command(self.LCD_DISPLAY_CTRL | self.display_ctrl)
    
    def backlight_on(self):
        """Увімкнути підсвітку"""
        self.backlight = True
        self.hal_backlight_on()
    
    def backlight_off(self):
        """Вимкнути підсвітку"""
        self.backlight = False
        self.hal_backlight_off()
    
    def move_to(self, cursor_x, cursor_y):
        """Перемістити курсор"""
        self.cursor_x = cursor_x
        self.cursor_y = cursor_y
        addr = cursor_x & 0x3f
        if cursor_y & 1:
            addr += 0x40
        if cursor_y & 2:
            addr += self.num_columns
        self.hal_write_command(self.LCD_DDRAM | addr)
    
    def putchar(self, char):
        """Вивести символ"""
        if char == '\n':
            self.cursor_x = self.num_columns
        else:
            self.hal_write_data(ord(char))
            self.cursor_x += 1
        
        if self.cursor_x >= self.num_columns:
            self.cursor_x = 0
            self.cursor_y += 1
            if self.cursor_y >= self.num_lines:
                self.cursor_y = 0
            self.move_to(self.cursor_x, self.cursor_y)
    
    def putstr(self, string):
        """Вивести рядок"""
        for char in string:
            self.putchar(char)
    
    # HAL методи - мають бути реалізовані в підкласах
    def hal_write_command(self, cmd):
        """Написати команду"""
        raise NotImplementedError
    
    def hal_write_data(self, data):
        """Написати дані"""
        raise NotImplementedError
    
    def hal_backlight_on(self):
        """Увімкнути підсвітку (HAL)"""
        pass
    
    def hal_backlight_off(self):
        """Вимкнути підсвітку (HAL)"""
        pass

