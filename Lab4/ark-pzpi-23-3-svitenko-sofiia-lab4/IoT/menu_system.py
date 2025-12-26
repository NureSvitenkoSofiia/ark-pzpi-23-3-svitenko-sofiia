"""
Система меню для IoT 3D Printer
"""

import time


class MenuSystem:
    """Система меню з навігацією"""
    
    # Стани меню
    STATE_MAIN_MENU = 'main_menu'
    STATE_POLLING = 'polling'
    STATE_STATISTICS = 'statistics'
    STATE_LANGUAGE = 'language'
    
    def __init__(self, lcd, localization, statistics):
        """
        Ініціалізація системи меню
        
        Args:
            lcd: Об'єкт LCD дисплея
            localization: Об'єкт локалізації
            statistics: Об'єкт статистики
        """
        self.lcd = lcd
        self.loc = localization
        self.stats = statistics
        
        self.current_state = self.STATE_MAIN_MENU
        self.selected_index = 0
        self.in_menu = True
        
        self.state_history = []
    
    def get_main_menu_items(self):
        """Отримати пункти головного меню"""
        return [
            self.loc.get('polling_mode'),
            self.loc.get('statistics'),
            self.loc.get('change_language'),
        ]
    
    def get_language_menu_items(self):
        """Отримати пункти меню вибору мови"""
        items = []
        for lang in self.loc.get_available_languages():
            name = self.loc.get_language_name(lang)
            if lang == self.loc.get_language():
                name = "> " + name
            items.append(name)
        return items
    
    def show_main_menu(self):
        """Показати головне меню"""
        items = self.get_main_menu_items()
        title = self.loc.get('main_menu')
        print("[Menu] Showing main menu: {} (LCD enabled: {})".format(title, self.lcd.enabled))
        self.lcd.show_menu(title, items, self.selected_index)
        if not self.lcd.enabled:
            self.lcd.print_console()
    
    def show_polling_mode(self, status, last_check=None):
        """
        Показати режим полінгу
        
        Args:
            status: Поточний статус принтера
            last_check: Час останньої перевірки
        """
        self.lcd.clear()
        self.lcd.write_center(0, self.loc.get('polling_mode'))
        self.lcd.write_center(1, self.loc.get('status_' + status))
        
        if last_check:
            elapsed = int(time.time() - last_check)
            self.lcd.write_line(2, "Last: {}s ago".format(elapsed))
        
        self.lcd.write_center(3, self.loc.get('press_exit'))
        
        if not self.lcd.enabled:
            self.lcd.print_console()
    
    def show_statistics(self):
        """Показати статистику"""
        stats = self.stats.get_summary()
        
        self.lcd.clear()
        self.lcd.write_center(0, self.loc.get('statistics'))
        
        line1 = "{}: {}".format(
            self.loc.get('total_jobs'),
            stats['total_jobs']
        )
        self.lcd.write_line(1, line1)
        
        # Рядок 2: Успішні/Невдалі
        line2 = "OK:{} Err:{}".format(
            stats['successful_jobs'],
            stats['failed_jobs']
        )
        self.lcd.write_line(2, line2)
        
        # Рядок 3: Матеріал і час
        line3 = "{}:{:.1f}g {}:{}{}".format(
            self.loc.get('material'),
            stats['total_material_grams'],
            self.loc.get('time_left'),
            stats['total_print_time_hours'],
            self.loc.get('hours')
        )
        self.lcd.write_line(3, line3)
        
        if not self.lcd.enabled:
            self.lcd.print_console()
    
    def show_language_menu(self):
        """Показати меню вибору мови"""
        items = self.get_language_menu_items()
        self.lcd.show_menu(
            self.loc.get('change_language'),
            items,
            self.selected_index
        )
        if not self.lcd.enabled:
            self.lcd.print_console()
    
    def show_printing_status(self, progress, current_layer, total_layers, time_left, material_used):
        """
        Показати статус друку
        
        Args:
            progress: Прогрес (0-100)
            current_layer: Поточний шар
            total_layers: Загальна кількість шарів
            time_left: Час що залишився (секунди)
            material_used: Використано матеріалу (грами)
        """
        details = {
            self.loc.get('layer'): "{}/{}".format(current_layer, total_layers),
            self.loc.get('time_left'): "{}{}".format(int(time_left / 60), self.loc.get('minutes')),
            self.loc.get('material'): "{:.1f}{}".format(material_used, self.loc.get('grams'))
        }
        
        self.lcd.show_progress(self.loc.get('printing'), progress, details)
        
        if not self.lcd.enabled:
            self.lcd.print_console()
    
    def handle_button_up(self):
        """Обробити натискання кнопки "Вгору" """
        if self.current_state == self.STATE_MAIN_MENU:
            items = self.get_main_menu_items()
            self.selected_index = (self.selected_index - 1) % len(items)
            self.show_main_menu()
        elif self.current_state == self.STATE_LANGUAGE:
            items = self.get_language_menu_items()
            self.selected_index = (self.selected_index - 1) % len(items)
            self.show_language_menu()
    
    def handle_button_down(self):
        """Обробити натискання кнопки "Вниз" """
        if self.current_state == self.STATE_MAIN_MENU:
            items = self.get_main_menu_items()
            self.selected_index = (self.selected_index + 1) % len(items)
            self.show_main_menu()
        elif self.current_state == self.STATE_LANGUAGE:
            items = self.get_language_menu_items()
            self.selected_index = (self.selected_index + 1) % len(items)
            self.show_language_menu()
    
    def handle_button_select(self):
        """Обробити натискання кнопки "Вибір" """
        if self.current_state == self.STATE_MAIN_MENU:
            # Перейти до вибраного пункту
            if self.selected_index == 0:  # Polling Mode
                self.state_history.append(self.current_state)
                self.current_state = self.STATE_POLLING
                self.in_menu = False
                return 'start_polling'
            elif self.selected_index == 1:  # Statistics
                self.state_history.append(self.current_state)
                self.current_state = self.STATE_STATISTICS
                self.show_statistics()
            elif self.selected_index == 2:  # Change Language
                self.state_history.append(self.current_state)
                self.current_state = self.STATE_LANGUAGE
                self.selected_index = 0
                self.show_language_menu()
        
        elif self.current_state == self.STATE_LANGUAGE:
            # Вибрати мову
            languages = self.loc.get_available_languages()
            if self.selected_index < len(languages):
                new_lang = languages[self.selected_index]
                self.loc.set_language(new_lang)
                self.handle_button_back()  # Повернутись до головного меню
        
        return None
    
    def handle_button_back(self):
        """Обробити натискання кнопки "Назад" """
        if self.current_state == self.STATE_POLLING:
            self.current_state = self.STATE_MAIN_MENU
            self.in_menu = True
            self.selected_index = 0
            self.show_main_menu()
            return 'stop_polling'
        
        elif len(self.state_history) > 0:
            self.current_state = self.state_history.pop()
            self.selected_index = 0
            
            if self.current_state == self.STATE_MAIN_MENU:
                self.show_main_menu()
        
        return None
    
    def is_in_polling_mode(self):
        """Перевірити чи в режимі полінгу"""
        return self.current_state == self.STATE_POLLING
    
    def is_in_menu(self):
        """Перевірити чи в меню"""
        return self.in_menu

