import 'package:shared_preferences/shared_preferences.dart';

class ServerConfig {
  static const String _serverIpKey = 'SERVER_IP_ADDRESS';
  static const String _defaultIp = 'http://10.0.2.2:5000';

  static String _currentIp = _defaultIp;

  static String get serverIp => _currentIp;

  static Future<void> init() async {
    final prefs = await SharedPreferences.getInstance();
    _currentIp = prefs.getString(_serverIpKey) ?? _defaultIp;
  }

  static Future<void> updateServerIp(String newIp) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_serverIpKey, newIp);
    _currentIp = newIp;
  }
}
