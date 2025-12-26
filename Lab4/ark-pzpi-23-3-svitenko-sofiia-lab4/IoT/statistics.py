"""
Локальна статистика для 3D принтера
"""

import time


class Statistics:
    """Локальна статистика роботи принтера"""
    
    def __init__(self):
        """Ініціалізація статистики"""
        self.start_time = time.time()
        
        self.total_jobs = 0
        self.successful_jobs = 0
        self.failed_jobs = 0
        
        self.total_print_time_seconds = 0
        self.total_material_grams = 0
        
        self.current_job_start_time = None
        self.current_job_estimated_time = 0
        self.current_job_estimated_material = 0
    
    def start_job(self, estimated_time_minutes, estimated_material_grams):
        """
        Почати нове завдання
        
        Args:
            estimated_time_minutes: Розрахований час друку
            estimated_material_grams: Розрахована витрата матеріалу
        """
        self.current_job_start_time = time.time()
        self.current_job_estimated_time = estimated_time_minutes * 60
        self.current_job_estimated_material = estimated_material_grams
    
    def finish_job(self, success, actual_material_grams=None):
        """
        Завершити завдання
        
        Args:
            success: Чи успішно завершено
            actual_material_grams: Фактична витрата матеріалу
        """
        self.total_jobs += 1
        
        if success:
            self.successful_jobs += 1
        else:
            self.failed_jobs += 1
        
        if self.current_job_start_time:
            actual_time = time.time() - self.current_job_start_time
            self.total_print_time_seconds += actual_time
        
        if actual_material_grams:
            self.total_material_grams += actual_material_grams
        elif self.current_job_estimated_material:
            self.total_material_grams += self.current_job_estimated_material
        
        self.current_job_start_time = None
        self.current_job_estimated_time = 0
        self.current_job_estimated_material = 0
    
    def get_uptime_seconds(self):
        """Отримати час роботи системи в секундах"""
        return time.time() - self.start_time
    
    def get_uptime_formatted(self):
        """
        Отримати форматований час роботи
        
        Returns:
            tuple: (години, хвилини)
        """
        uptime = self.get_uptime_seconds()
        hours = int(uptime // 3600)
        minutes = int((uptime % 3600) // 60)
        return hours, minutes
    
    def get_total_print_time_formatted(self):
        """
        Отримати форматований загальний час друку
        
        Returns:
            tuple: (години, хвилини)
        """
        hours = int(self.total_print_time_seconds // 3600)
        minutes = int((self.total_print_time_seconds % 3600) // 60)
        return hours, minutes
    
    def get_summary(self):
        """
        Отримати повну статистику
        
        Returns:
            dict: Словник зі статистикою
        """
        uptime_hours, uptime_minutes = self.get_uptime_formatted()
        print_hours, print_minutes = self.get_total_print_time_formatted()
        
        return {
            'total_jobs': self.total_jobs,
            'successful_jobs': self.successful_jobs,
            'failed_jobs': self.failed_jobs,
            'success_rate': (self.successful_jobs / self.total_jobs * 100) if self.total_jobs > 0 else 0,
            'total_print_time_hours': print_hours,
            'total_print_time_minutes': print_minutes,
            'total_material_grams': self.total_material_grams,
            'uptime_hours': uptime_hours,
            'uptime_minutes': uptime_minutes
        }

