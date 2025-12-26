"""
Обробник кнопок для IoT 3D Printer
"""

try:
    from machine import Pin
    HAS_BUTTONS = True
except ImportError:
    HAS_BUTTONS = False
    print("[Buttons] Hardware not available")

import time


class ButtonHandler:
    """Обробник кнопок навігації"""
    
    DEBOUNCE_MS = 200
    
    def __init__(self, pin_up=15, pin_down=2, pin_select=4, pin_back=5):
        """
        Ініціалізація обробника кнопок
        
        Args:
            pin_up: Pin для кнопки "Вгору"
            pin_down: Pin для кнопки "Вниз"
            pin_select: Pin для кнопки "Вибір"
            pin_back: Pin для кнопки "Назад"
        """
        self.enabled = False
        self.buttons = {}
        self.last_press_time = {}
        
        if HAS_BUTTONS:
            try:
                self.buttons = {
                    'up': Pin(pin_up, Pin.IN, Pin.PULL_UP),
                    'down': Pin(pin_down, Pin.IN, Pin.PULL_UP),
                    'select': Pin(pin_select, Pin.IN, Pin.PULL_UP),
                    'back': Pin(pin_back, Pin.IN, Pin.PULL_UP)
                }
                
                for key in self.buttons:
                    self.last_press_time[key] = 0
                
                self.enabled = True
                print("[Buttons] Initialized on pins: UP={}, DOWN={}, SELECT={}, BACK={}".format(
                    pin_up, pin_down, pin_select, pin_back
                ))
            except Exception as e:
                print("[Buttons] Failed to initialize: {}".format(e))
                self.enabled = False
    
    def _is_debounced(self, button_name):
        """
        Перевірити debounce для кнопки
        
        Args:
            button_name: Назва кнопки
            
        Returns:
            bool: True якщо можна обробити натискання
        """
        current_time = time.ticks_ms()
        last_time = self.last_press_time.get(button_name, 0)
        
        if time.ticks_diff(current_time, last_time) > self.DEBOUNCE_MS:
            self.last_press_time[button_name] = current_time
            return True
        
        return False
    
    def is_pressed(self, button_name):
        """
        Перевірити чи натиснута кнопка
        
        Args:
            button_name: Назва кнопки ('up', 'down', 'select', 'back')
            
        Returns:
            bool: True якщо кнопка натиснута
        """
        if not self.enabled or button_name not in self.buttons:
            return False
        
        if self.buttons[button_name].value() == 0:
            if self._is_debounced(button_name):
                return True
        
        return False
    
    def get_pressed_button(self):
        """
        Отримати назву натиснутої кнопки
        
        Returns:
            str or None: Назва кнопки або None
        """
        for button_name in ['up', 'down', 'select', 'back']:
            if self.is_pressed(button_name):
                return button_name
        
        return None
    
    def wait_for_button(self, timeout_ms=None):
        """
        Очікувати натискання будь-якої кнопки
        
        Args:
            timeout_ms: Таймаут в мілісекундах (None - необмежено)
            
        Returns:
            str or None: Назва натиснутої кнопки або None (таймаут)
        """
        start_time = time.ticks_ms()
        
        while True:
            button = self.get_pressed_button()
            if button:
                return button
            
            if timeout_ms:
                if time.ticks_diff(time.ticks_ms(), start_time) > timeout_ms:
                    return None
            
            time.sleep_ms(50)

