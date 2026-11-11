# Coursifix
Der Kurs-Companion

## Einrichtung
Alle Einstellungen sind in der preferences.json zu finden.
```json
{
  "Course": {
    "Course": "[Kursname z.b. MA-TINF26IT1]",
    "UpdateInterval": "00:30:00",
    "WlanUserName": "[DHBW Benutzername]",
    "WlanPassword": "[Passwort]",
    "LoginUri": "http://dhbwwebauth2.dhbw-mannheim.de/login.html",
    "LoginRedirectUri": "http://www.msftconnecttest.com/redirect"
  },
  "Moodle": {
    "StartPath": "/my/courses.php",
    "MoodleHost": "moodle.dhbw.de",
    "Organization": "student.dhbw-mannheim.de"
  }
}
```
Parameter welche vor dem ersten Start konfiguriert werden sollten, sind mit ``[]`` gekennzeichnet.