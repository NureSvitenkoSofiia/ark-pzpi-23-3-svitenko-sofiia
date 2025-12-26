"""
Printer Controller для симуляції/керування 3D принтером
"""

import time


class PrinterController:
    """Контролер для керування 3D принтером (симуляція)"""
    
    # Множник швидкості симуляції (більше = швидше)
    SIMULATION_SPEED_MULTIPLIER = 3
    
    def __init__(self):
        self.is_printing = False
        self.current_job_id = None
        self.print_start_time = None
        self.estimated_duration = 0
        self.total_layers = 0
        self.current_layer = 0
        self.estimated_material = 0
        self.used_material = 0
        
        # Температури
        self.nozzle_temp = 0
        self.bed_temp = 0
        self.target_nozzle_temp = 0
        self.target_bed_temp = 0
    
    def start_print(self, job_id, estimated_time_minutes, estimated_material_grams, total_layers):
        """
        Почати друк
        
        Args:
            job_id: ID завдання
            estimated_time_minutes: Розрахований час друку
            estimated_material_grams: Розрахована витрата матеріалу
            total_layers: Загальна кількість шарів
        """
        print("[Printer] Starting print job {}".format(job_id))
        
        self.is_printing = True
        self.current_job_id = job_id
        self.print_start_time = time.time()
        self.estimated_duration = (estimated_time_minutes * 60) / self.SIMULATION_SPEED_MULTIPLIER
        self.total_layers = total_layers
        self.current_layer = 0
        self.estimated_material = estimated_material_grams
        self.used_material = 0
        
        print("[Printer] Simulation speed: {}x ({}min -> {:.1f}min)".format(
            self.SIMULATION_SPEED_MULTIPLIER,
            estimated_time_minutes,
            self.estimated_duration / 60
        ))
        
        # Підігрів (симуляція)
        self._heat_up()
    
    def _heat_up(self):
        """Симуляція підігріву"""
        self.target_nozzle_temp = 210  # °C для PLA
        self.target_bed_temp = 60      # °C для PLA
        
        print("[Printer] Heating up...")
        print("  - Nozzle target: {}°C".format(self.target_nozzle_temp))
        print("  - Bed target: {}°C".format(self.target_bed_temp))
        
        # Симуляція підігріву (прискорена)
        for i in range(5):
            self.nozzle_temp = self.target_nozzle_temp * (i + 1) / 5
            self.bed_temp = self.target_bed_temp * (i + 1) / 5
            print("  - Heating... Nozzle: {:.0f}°C, Bed: {:.0f}°C".format(self.nozzle_temp, self.bed_temp))
            time.sleep(0.2)  # Прискорений підігрів
        
        self.nozzle_temp = self.target_nozzle_temp
        self.bed_temp = self.target_bed_temp
        print("[Printer] Ready to print!")
    
    def get_status(self):
        """
        Отримати поточний статус друку
        
        Returns:
            dict: Статус друку
        """
        if not self.is_printing:
            return {
                'is_printing': False,
                'progress': 0,
                'elapsed_time': 0,
                'remaining_time': 0,
                'current_layer': 0,
                'total_layers': 0
            }
        
        elapsed_time = time.time() - self.print_start_time
        progress = min((elapsed_time / self.estimated_duration) * 100, 100) if self.estimated_duration > 0 else 0
        remaining_time = max(self.estimated_duration - elapsed_time, 0)
        
        # Симуляція прогресу шарів
        self.current_layer = int((progress / 100) * self.total_layers)
        
        # Симуляція витрати матеріалу
        self.used_material = (progress / 100) * self.estimated_material
        
        return {
            'is_printing': True,
            'progress': progress,
            'elapsed_time': elapsed_time,
            'remaining_time': remaining_time,
            'current_layer': self.current_layer,
            'total_layers': self.total_layers,
            'nozzle_temp': self.nozzle_temp,
            'bed_temp': self.bed_temp,
            'used_material': self.used_material
        }
    
    def is_complete(self):
        """Перевірити чи завершено друк"""
        if not self.is_printing:
            return False
        
        status = self.get_status()
        return status['progress'] >= 100
    
    def stop_print(self):
        """Зупинити друк"""
        print("[Printer] Stopping print...")
        
        self.is_printing = False
        final_material = self.used_material
        
        # Охолодження
        self._cool_down()
        
        return final_material
    
    def _cool_down(self):
        """Симуляція охолодження"""
        print("[Printer] Cooling down...")
        
        self.target_nozzle_temp = 0
        self.target_bed_temp = 0
        
        # Швидке охолодження для тесту
        self.nozzle_temp = 0
        self.bed_temp = 0
        
        print("[Printer] Cooled down")
    
    def simulate_error(self):
        """Симуляція помилки друку (для тестування)"""
        return False  # За замовчуванням немає помилок

