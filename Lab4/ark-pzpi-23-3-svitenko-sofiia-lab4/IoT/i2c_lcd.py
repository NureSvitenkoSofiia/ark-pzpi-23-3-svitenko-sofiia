"""
I2C LCD драйвер для MicroPython
Підтримує LCD дисплеї з I2C адаптером (PCF8574)
"""

import time
from lcd_api import LcdApi


class I2cLcd(LcdApi):
    """I2C LCD драйвер"""
    
    # Маски для I2C адаптера PCF8574
    MASK_RS = 0x01
    MASK_RW = 0x02
    MASK_E = 0x04
    MASK_BACKLIGHT = 0x08
    
    def __init__(self, i2c, i2c_addr, num_lines, num_columns):
        """
        Ініціалізація I2C LCD
        
        Args:
            i2c: Об'єкт I2C
            i2c_addr: I2C адреса
            num_lines: Кількість рядків
            num_columns: Кількість стовпців
        """
        self.i2c = i2c
        self.i2c_addr = i2c_addr
        self.backlight_value = self.MASK_BACKLIGHT
        
        # Ініціалізація LCD в 4-бітному режимі (HD44780)
        time.sleep_ms(50)  # Почекати >40ms після VCC rises to 2.7V
        
        self.hal_write_init_nibble(0x03)
        time.sleep_ms(5)  # >4.1ms
        
        self.hal_write_init_nibble(0x03)
        time.sleep_ms(5)  # >100us
        
        self.hal_write_init_nibble(0x03)
        time.sleep_ms(1)
        
        self.hal_write_init_nibble(0x02)  # Встановити 4-бітний режим
        time.sleep_ms(2)
        
        # Викликати батьківський конструктор
        super().__init__(num_lines, num_columns)
        
        # Налаштування LCD функцій
        cmd = self.LCD_FUNCTION | self.LCD_4BITMODE | self.LCD_2LINE | self.LCD_5x8DOTS
        self.hal_write_command(cmd)
        time.sleep_ms(2)
        
        # Увімкнути дисплей
        self.hal_write_command(self.LCD_DISPLAY_CTRL | self.LCD_DISPLAY_ON)
        time.sleep_ms(2)
        
        # Очистити дисплей
        self.hal_write_command(self.LCD_CLR)
        time.sleep_ms(3)  # Clear потребує більше часу
        
        # Встановити режим вводу
        self.hal_write_command(self.LCD_ENTRY_MODE | self.LCD_ENTRY_LEFT | self.LCD_ENTRY_SHIFT_DECREMENT)
        time.sleep_ms(2)
    
    def hal_write_init_nibble(self, nibble):
        """Написати 4 біти під час ініціалізації"""
        byte = ((nibble >> 0) & 0x01) << 4
        byte |= ((nibble >> 1) & 0x01) << 5
        byte |= ((nibble >> 2) & 0x01) << 6
        byte |= ((nibble >> 3) & 0x01) << 7
        self.i2c.writeto(self.i2c_addr, bytearray([byte | self.backlight_value]))
        time.sleep_ms(1)
        self.i2c.writeto(self.i2c_addr, bytearray([byte | self.MASK_E | self.backlight_value]))
        time.sleep_ms(1)
        self.i2c.writeto(self.i2c_addr, bytearray([byte | self.backlight_value]))
        time.sleep_ms(1)
    
    def hal_backlight_on(self):
        """Увімкнути підсвітку"""
        self.backlight_value = self.MASK_BACKLIGHT
        self.i2c.writeto(self.i2c_addr, bytearray([self.backlight_value]))
    
    def hal_backlight_off(self):
        """Вимкнути підсвітку"""
        self.backlight_value = 0
        self.i2c.writeto(self.i2c_addr, bytearray([self.backlight_value]))
    
    def hal_write_command(self, cmd):
        """Написати команду"""
        self.hal_write_byte(cmd, 0)
    
    def hal_write_data(self, data):
        """Написати дані"""
        self.hal_write_byte(data, self.MASK_RS)
    
    def hal_write_byte(self, value, mode):
        """Написати байт у 4-бітному режимі"""
        # Верхній nibble
        byte = mode | (value & 0xF0) | self.backlight_value
        self.i2c.writeto(self.i2c_addr, bytearray([byte]))
        self.hal_pulse_enable(byte)
        
        # Нижній nibble
        byte = mode | ((value << 4) & 0xF0) | self.backlight_value
        self.i2c.writeto(self.i2c_addr, bytearray([byte]))
        self.hal_pulse_enable(byte)
        
        if mode == 0:  # Команда
            time.sleep_ms(2)
    
    def hal_pulse_enable(self, data):
        """Імпульс на лінії Enable"""
        self.i2c.writeto(self.i2c_addr, bytearray([data | self.MASK_E]))
        time.sleep_us(500)  # Enable pulse must be >450ns
        self.i2c.writeto(self.i2c_addr, bytearray([data & ~self.MASK_E]))
        time.sleep_us(100)  # Commands need > 37us to settle

