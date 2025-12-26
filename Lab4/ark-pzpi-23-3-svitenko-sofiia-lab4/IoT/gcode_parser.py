"""
GCode Parser для розрахунку часу друку та витрати матеріалу
"""

import math


class GCodeParser:
    """Парсер для аналізу GCode файлів"""
    
    def __init__(self):
        self.reset()
    
    def reset(self):
        """Скинути стан парсера"""
        self.total_extrusion = 0.0  # Загальна екструзія (mm)
        self.total_time = 0.0  # Загальний час (секунди)
        self.current_position = {'X': 0.0, 'Y': 0.0, 'Z': 0.0, 'E': 0.0}
        self.current_feedrate = 1500.0  # mm/min (за замовчуванням)
        self.layer_count = 0
    
    def parse_file(self, filepath):
        """
        Парсить GCode файл і розраховує estimate
        
        Args:
            filepath: Шлях до GCode файлу
            
        Returns:
            dict: {'time_minutes': float, 'extrusion_mm': float, 'layers': int}
        """
        self.reset()
        
        print("[GCode] Parsing: {}".format(filepath))
        
        try:
            with open(filepath, 'r') as f:
                line_count = 0
                for line in f:
                    line = line.strip()
                    if line and not line.startswith(';'):
                        self._parse_line(line)
                        line_count += 1
                        
                        # Друкуємо прогрес кожні 10000 ліній
                        if line_count % 10000 == 0:
                            print("[GCode] Processed {} lines...".format(line_count))
            
            time_minutes = self.total_time / 60.0
            
            print("[GCode] Parsing complete:")
            print("  - Lines: {}".format(line_count))
            print("  - Time: {:.2f} min".format(time_minutes))
            print("  - Extrusion: {:.2f} mm".format(self.total_extrusion))
            print("  - Layers: {}".format(self.layer_count))
            
            return {
                'time_minutes': time_minutes,
                'extrusion_mm': self.total_extrusion,
                'layers': self.layer_count
            }
            
        except Exception as e:
            print("[GCode] Parse error: {}".format(e))
            return None
    
    def _parse_line(self, line):
        """Парсить одну лінію GCode (оптимізовано)"""
        # Видаляємо коментарі
        if ';' in line:
            line = line.split(';')[0].strip()
        
        if not line:
            return
        
        # Швидка перевірка перших символів
        if not (line[0] == 'G' or line[0] == 'g'):
            return
        
        parts = line.split()
        if not parts:
            return
        
        command = parts[0].upper()
        
        # G0/G1 - Рух (найчастіша команда)
        if command == 'G0' or command == 'G1':
            self._parse_move(parts)
            # Перевірка Z для layer count
            if 'Z' in line.upper():
                self.layer_count += 1
    
    def _parse_move(self, parts):
        """Парсить команду руху G0/G1 (оптимізовано)"""
        new_position = self.current_position.copy()
        new_feedrate = self.current_feedrate
        
        # Парсимо параметри (оптимізовано)
        for part in parts[1:]:
            if len(part) < 2:
                continue
            
            axis = part[0].upper()
            
            # Швидка перевірка осей
            if axis == 'X' or axis == 'Y' or axis == 'Z' or axis == 'E':
                try:
                    new_position[axis] = float(part[1:])
                except:
                    continue
            elif axis == 'F':
                try:
                    new_feedrate = float(part[1:])
                except:
                    continue
        
        # Розрахунок відстані та часу
        distance = self._calculate_distance(
            self.current_position,
            new_position
        )
        
        if distance > 0:
            time_seconds = (distance / new_feedrate) * 60.0
            self.total_time += time_seconds
        
        # Розрахунок екструзії
        extrusion_delta = new_position['E'] - self.current_position['E']
        if extrusion_delta > 0:
            self.total_extrusion += extrusion_delta
        
        # Оновлюємо поточну позицію
        self.current_position = new_position
        self.current_feedrate = new_feedrate
    
    def _calculate_distance(self, pos1, pos2):
        """Розраховує 3D відстань між двома точками"""
        dx = pos2['X'] - pos1['X']
        dy = pos2['Y'] - pos1['Y']
        dz = pos2['Z'] - pos1['Z']
        
        return math.sqrt(dx*dx + dy*dy + dz*dz)
    
    def calculate_material_weight(self, extrusion_mm, filament_diameter_mm, density_g_cm3):
        """
        Розраховує вагу матеріалу
        
        Args:
            extrusion_mm: Довжина екструзії (мм)
            filament_diameter_mm: Діаметр філаменту (мм)
            density_g_cm3: Щільність матеріалу (г/см³)
            
        Returns:
            float: Вага в грамах
        """
        # Об'єм циліндра філаменту: V = π * r² * L
        radius_mm = filament_diameter_mm / 2.0
        volume_mm3 = math.pi * (radius_mm ** 2) * extrusion_mm
        
        # Конвертуємо мм³ в см³
        volume_cm3 = volume_mm3 / 1000.0
        
        # Вага = об'єм * щільність
        weight_grams = volume_cm3 * density_g_cm3
        
        return weight_grams

