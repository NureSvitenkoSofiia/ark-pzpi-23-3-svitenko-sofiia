"""
Система локалізації для IoT 3D Printer
Підтримує англійську та українську мови (ASCII транслітерація)
"""


class Localization:
    """Система локалізації з підтримкою декількох мов"""
    
    # Словники локалізації
    TRANSLATIONS = {
        'en': {
            # Головне меню
            'main_menu': 'Main Menu',
            'polling_mode': 'Polling Mode',
            'statistics': 'Statistics',
            'change_language': 'Change Language',
            'back': 'Back',
            'exit': 'Exit',
            
            # Режим полінгу
            'polling': 'Polling...',
            'waiting_job': 'Waiting for job',
            'press_exit': 'Press to exit',
            'idle_state': 'IDLE',
            
            # Статус принтера
            'status_idle': 'Idle',
            'status_printing': 'Printing',
            'status_error': 'Error',
            'status_heating': 'Heating',
            
            # Друк
            'printing': 'Printing',
            'progress': 'Progress',
            'layer': 'Layer',
            'time_left': 'Time left',
            'material': 'Material',
            'job': 'Job',
            
            # Статистика
            'total_jobs': 'Total jobs',
            'successful': 'Successful',
            'failed': 'Failed',
            'total_time': 'Total time',
            'total_material': 'Total material',
            'uptime': 'Uptime',
            'minutes': 'min',
            'hours': 'h',
            'grams': 'g',
            
            # Температури
            'nozzle': 'Nozzle',
            'bed': 'Bed',
            'temp': 'Temp',
            
            # Мережа
            'wifi_connecting': 'WiFi connecting',
            'wifi_connected': 'WiFi connected',
            'wifi_failed': 'WiFi failed',
            'ip': 'IP',
            
            # Система
            'system_ready': 'System Ready',
            'initializing': 'Initializing',
            'error': 'Error',
            'warning': 'Warning',
            'info': 'Info',
            
            # Кнопки
            'btn_select': 'Select',
            'btn_up': 'Up',
            'btn_down': 'Down',
            'btn_back': 'Back',
        },
        
        'ua': {  # Українська (ASCII транслітерація)
            # Головне меню
            'main_menu': 'Holovne Menyu',
            'polling_mode': 'Rezhym Polinhu',
            'statistics': 'Statystyka',
            'change_language': 'Zmina Movy',
            'back': 'Nazad',
            'exit': 'Vykhid',
            
            # Режим полінгу
            'polling': 'Polinh...',
            'waiting_job': 'Ochikuvannya zavdannya',
            'press_exit': 'Natysnit vykhid',
            'idle_state': 'OCHIKUYE',
            
            # Статус принтера
            'status_idle': 'Ochikuye',
            'status_printing': 'Drukuye',
            'status_error': 'Pomylka',
            'status_heating': 'Nahrivannya',
            
            # Друк
            'printing': 'Druk',
            'progress': 'Prohres',
            'layer': 'Shar',
            'time_left': 'Zalyshylos',
            'material': 'Material',
            'job': 'Zavdannya',
            
            # Статистика
            'total_jobs': 'Vsoho zavdan',
            'successful': 'Uspishno',
            'failed': 'Pomylok',
            'total_time': 'Chas',
            'total_material': 'Material',
            'uptime': 'Chas roboty',
            'minutes': 'khv',
            'hours': 'hod',
            'grams': 'h',
            
            # Температури
            'nozzle': 'Dzyurka',
            'bed': 'Stol',
            'temp': 'Temp',
            
            # Мережа
            'wifi_connecting': 'WiFi pidklyuchennya',
            'wifi_connected': 'WiFi pidklyucheno',
            'wifi_failed': 'WiFi pomylka',
            'ip': 'IP',
            
            # Система
            'system_ready': 'Systema hotova',
            'initializing': 'Initsializatsiya',
            'error': 'Pomylka',
            'warning': 'Uvaha',
            'info': 'Info',
            
            # Кнопки
            'btn_select': 'Vybir',
            'btn_up': 'Vhoru',
            'btn_down': 'Vnyz',
            'btn_back': 'Nazad',
        }
    }
    
    def __init__(self, default_language='en'):
        """
        Ініціалізація системи локалізації
        
        Args:
            default_language: Мова за замовчуванням ('en' або 'ua')
        """
        self.current_language = default_language
        self.available_languages = list(self.TRANSLATIONS.keys())
    
    def get(self, key, default=None):
        """
        Отримати переклад за ключем
        
        Args:
            key: Ключ перекладу
            default: Значення за замовчуванням
            
        Returns:
            str: Перекладений текст
        """
        translations = self.TRANSLATIONS.get(self.current_language, {})
        return translations.get(key, default or key)
    
    def set_language(self, language):
        """
        Встановити мову
        
        Args:
            language: Код мови ('en' або 'ua')
        """
        if language in self.available_languages:
            self.current_language = language
            return True
        return False
    
    def get_language(self):
        """Отримати поточну мову"""
        return self.current_language
    
    def get_available_languages(self):
        """Отримати список доступних мов"""
        return self.available_languages
    
    def get_language_name(self, lang_code):
        """Отримати повну назву мови"""
        names = {
            'en': 'English',
            'ua': 'Ukrainska'
        }
        return names.get(lang_code, lang_code)

