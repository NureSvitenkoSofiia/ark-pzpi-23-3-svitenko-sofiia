"""
Конфігурація IoT 3D Printer Client
"""

# API Configuration
API_BASE_URL = "http://74.248.152.8"
PRINTER_ID = 1  # ID принтера в системі

# WiFi Configuration
WIFI_SSID = "Wokwi-GUEST"
WIFI_PASSWORD = ""

# Timing Configuration (seconds)
PING_INTERVAL = 120  # Інтервал відправки ping
QUEUE_POLL_INTERVAL = 10  # Інтервал перевірки черги
PRINT_MONITORING_INTERVAL = 2  # Інтервал моніторингу друку

# GCode Processing
MAX_GCODE_LINES_TO_PARSE = 50000  # Якщо більше - використати estimate з сервера
SKIP_GCODE_PARSING = False  # True - завжди використовувати estimate з сервера

# Printer Status
STATUS_IDLE = "idle"
STATUS_PRINTING = "printing"
STATUS_ERROR = "error"

# File Paths
# MicroPython не має /tmp, використовуємо поточну директорію
TEMP_DIR = "."
GCODE_CACHE_FILE = "current.gcode"

# UI Configuration
LCD_I2C_ADDR = 0x27  # I2C адреса LCD (зазвичай 0x27 або 0x3F)
LCD_SCL_PIN = 22     # Pin для I2C SCL
LCD_SDA_PIN = 21     # Pin для I2C SDA

# Button Pins
BTN_UP_PIN = 15      # Pin для кнопки "Вгору"
BTN_DOWN_PIN = 2     # Pin для кнопки "Вниз"
BTN_SELECT_PIN = 4   # Pin для кнопки "Вибір"
BTN_BACK_PIN = 5     # Pin для кнопки "Назад"

# Default Language ('en' або 'ua')
DEFAULT_LANGUAGE = 'en'

