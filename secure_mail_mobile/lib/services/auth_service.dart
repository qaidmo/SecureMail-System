import 'dart:async';
import 'dart:io';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import '../core/server_config.dart';

class AuthService {
  final _storage = const FlutterSecureStorage();
  static const String _tokenKey = 'SESSION_TOKEN';

  String get _baseUrl => '${ServerConfig.serverIp}/api/auth';

  Future<Map<String, dynamic>> register({
    required String fullName,
    required String email,
    required String password,
    String? phone,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$_baseUrl/register'),
        headers: {'Content-Type': 'application/json'},
        body: json.encode({
          'fullName': fullName,
          'email': email,
          'password': password,
          'confirmPassword': password, // Needed by backend DTO
          'phone': phone,
        }),
      ).timeout(const Duration(seconds: 10));

      // Log detailed error information for debugging
      if (response.statusCode != 200) {
        print('Registration Failed (Status: ${response.statusCode})');
        print('Response Body: ${response.body}');
      }

      final data = json.decode(response.body);

      if (response.statusCode == 200) {
        return {'success': true, 'message': data['message']};
      } else {
        // Try to handle validation errors returned by API
        String errorMsg = data['message'] ?? 'فشل التسجيل (Registration failed)';
        if (data['errors'] != null && data['errors'] is Map) {
            final Map errors = data['errors'];
            if (errors.isNotEmpty) {
                errorMsg = errors.values.first.first.toString();
            }
        }
        return {'success': false, 'message': errorMsg};
      }
    } on TimeoutException {
      return {'success': false, 'message': 'Connection timed out. Please check the Server IP.'};
    } on SocketException {
      return {'success': false, 'message': 'Connection timed out. Please check the Server IP.'};
    } catch (e) {
      print('Network Error during registration: $e');
      return {'success': false, 'message': 'تعذر الاتصال بالخادم. تأكد من إعدادات السيرفر ($e)'};
    }
  }

  Future<Map<String, dynamic>> verifyOtp({
    required String email,
    required String otpCode,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$_baseUrl/verify'),
        headers: {'Content-Type': 'application/json'},
        body: json.encode({
          'email': email,
          'otpCode': otpCode,
        }),
      );

      final data = json.decode(response.body);

      if (response.statusCode == 200) {
        return {'success': true, 'message': data['message']};
      } else {
        return {'success': false, 'message': data['message'] ?? 'Verification failed'};
      }
    } catch (e) {
      return {'success': false, 'message': 'Network error occurred: $e'};
    }
  }

  Future<Map<String, dynamic>> login({
    required String email,
    required String password,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$_baseUrl/login'),
        headers: {'Content-Type': 'application/json'},
        body: json.encode({
          'email': email,
          'password': password,
        }),
      ).timeout(const Duration(seconds: 10));

      final data = json.decode(response.body);

      if (response.statusCode == 200) {
        // Save the token
        final token = data['token'];
        if (token != null) {
          await _storage.write(key: _tokenKey, value: token);
          try {
            final fcmToken = await FirebaseMessaging.instance.getToken();
            if (fcmToken != null) await updateDeviceToken(fcmToken);
          } catch (e) {}
        }
        return {'success': true, 'message': data['message']};
      } else {
        return {'success': false, 'message': data['message'] ?? 'Login failed'};
      }
    } on TimeoutException {
      return {'success': false, 'message': 'Connection timed out. Please check the Server IP.'};
    } on SocketException {
      return {'success': false, 'message': 'Connection timed out. Please check the Server IP.'};
    } catch (e) {
      return {'success': false, 'message': 'Network error occurred: $e'};
    }
  }

  Future<String?> getToken() async {
    return await _storage.read(key: _tokenKey);
  }

  Future<void> logout() async {
    await _storage.delete(key: _tokenKey);
  }

  Future<void> updateDeviceToken(String fcmToken) async {
    try {
      final sessionToken = await getToken();
      if (sessionToken == null) return;
      
      await http.post(
        Uri.parse('${ServerConfig.serverIp}/api/user/device-token'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $sessionToken',
        },
        body: json.encode({
          'fcmToken': fcmToken,
          'platform': 'android',
        }),
      ).timeout(const Duration(seconds: 10));
    } on TimeoutException {
      print('Connection timed out updating FCM token');
    } on SocketException {
      print('Network error updating FCM token');
    } catch (e) {
      print('Failed to sync fcm token: $e');
    }
  }
}
