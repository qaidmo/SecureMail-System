import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../core/server_config.dart';

class DashboardService {
  final _storage = const FlutterSecureStorage();
  
  String get _baseUrl => ServerConfig.serverIp;

  Future<String?> _getToken() async {
    return await _storage.read(key: 'SESSION_TOKEN');
  }

  Future<Map<String, String>> _getHeaders() async {
    final token = await _getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Future<Map<String, dynamic>> getIntegrationStatus() async {
    try {
      final headers = await _getHeaders();
      final response = await http.get(
        Uri.parse('$_baseUrl/api/integration/status'),
        headers: headers,
      );

      if (response.statusCode == 200) {
        return json.decode(response.body);
      }
      return {'isLinked': false};
    } catch (e) {
      return {'isLinked': false, 'error': e.toString()};
    }
  }

  Future<List<dynamic>> getRecentScans({int? integrationId}) async {
    try {
      final headers = await _getHeaders();
      String url = '$_baseUrl/api/dashboard/scans';
      if (integrationId != null) {
        url += '?integrationId=$integrationId';
      }
      final response = await http.get(
        Uri.parse(url),
        headers: headers,
      );

      if (response.statusCode == 200) {
        return json.decode(response.body);
      }
      return [];
    } catch (e) {
      return [];
    }
  }

  /// Fetches the current user's profile data including connected integrations.
  Future<Map<String, dynamic>?> getUserProfile() async {
    try {
      final headers = await _getHeaders();
      final response = await http.get(
        Uri.parse('$_baseUrl/api/user/profile'),
        headers: headers,
      );

      if (response.statusCode == 200) {
        return json.decode(response.body);
      }
      return null;
    } catch (e) {
      return null;
    }
  }

  Future<Map<String, dynamic>?> getScanDetails(String scanId) async {
    try {
      final headers = await _getHeaders();
      final response = await http.get(
        Uri.parse('$_baseUrl/api/dashboard/scan/$scanId'),
        headers: headers,
      );

      if (response.statusCode == 200) {
        return json.decode(response.body);
      }
      return null;
    } catch (e) {
      return null;
    }
  }

  Future<Map<String, dynamic>> analyzeEmail(String email) async {
    try {
      final headers = await _getHeaders();
      final response = await http.get(
        Uri.parse('$_baseUrl/api/analyzer/email?email=$email'),
        headers: headers,
      );

      if (response.statusCode == 200) {
        return {'success': true, 'data': json.decode(response.body)};
      } else {
        final error = json.decode(response.body);
        return {'success': false, 'message': error['message'] ?? 'فشل التحليل'};
      }
    } catch (e) {
      return {'success': false, 'message': 'حدث خطأ في الشبكة: $e'};
    }
  }

  // ============================================
  // IMAP INTEGRATION (NEW — Web Parity)
  // ============================================

  /// Connects an IMAP account. Supports optional manual host/port override.
  Future<Map<String, dynamic>> connectImap({
    required String email,
    required String password,
    String? imapHost,
    int? imapPort,
  }) async {
    try {
      final headers = await _getHeaders();
      final Map<String, dynamic> payload = {
        'email': email,
        'password': password,
      };
      if (imapHost != null && imapHost.trim().isNotEmpty) {
        payload['imapHost'] = imapHost.trim();
      }
      if (imapPort != null) {
        payload['imapPort'] = imapPort;
      }

      final response = await http.post(
        Uri.parse('$_baseUrl/api/integration/imap/connect'),
        headers: headers,
        body: json.encode(payload),
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {'success': true, 'message': data['message'] ?? 'تم ربط الحساب بنجاح!'};
      } else {
        final error = json.decode(response.body);
        return {'success': false, 'message': error['message'] ?? 'فشل الاتصال بـ IMAP'};
      }
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال بالخادم: $e'};
    }
  }

  /// Unlinks a connected integration by its ID.
  Future<Map<String, dynamic>> unlinkIntegration(int integrationId) async {
    try {
      final headers = await _getHeaders();
      final response = await http.delete(
        Uri.parse('$_baseUrl/api/integration/unlink/$integrationId'),
        headers: headers,
      );

      if (response.statusCode == 200) {
        return {'success': true, 'message': 'تم إلغاء ربط الحساب بنجاح'};
      } else {
        return {'success': false, 'message': 'حدث خطأ أثناء إلغاء الربط'};
      }
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال: $e'};
    }
  }
}
