@echo off
echo ====================================
echo IoT 3D Printer - Loading files...
echo ====================================
echo.

echo Copying files to ESP32...
python -m mpremote connect port:rfc2217://localhost:4000 fs cp config.py :config.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp api_client.py :api_client.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp gcode_parser.py :gcode_parser.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp printer_controller.py :printer_controller.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp localization.py :localization.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp statistics.py :statistics.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp lcd_api.py :lcd_api.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp i2c_lcd.py :i2c_lcd.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp lcd_display.py :lcd_display.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp button_handler.py :button_handler.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp menu_system.py :menu_system.py
python -m mpremote connect port:rfc2217://localhost:4000 fs cp main.py :main.py

echo.
echo Files copied successfully!
echo.
echo Performing soft reset to clear cache...
python -m mpremote connect port:rfc2217://localhost:4000 soft-reset

timeout /t 2 /nobreak >nul 2>&1

echo.
echo Starting main application...
python -m mpremote connect port:rfc2217://localhost:4000 exec "import main"