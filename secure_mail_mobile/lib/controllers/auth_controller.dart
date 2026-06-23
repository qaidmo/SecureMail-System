import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../models/user_model.dart';
import '../widgets/top_notification.dart';
import '../core/server_config.dart';

class AuthController extends ChangeNotifier {
  final AuthService _authService = AuthService();
  
  UserModel? _currentUser;
  bool _isLoading = false;

  UserModel? get currentUser => _currentUser;
  bool get isLoading => _isLoading;

  Future<bool> login(BuildContext context, String email, String password) async {
    _setLoading(true);
    final result = await _authService.login(email: email, password: password);
    _setLoading(false);

    if (result['success']) {
      final Map<String, dynamic> data = result['data'] ?? {};
      _currentUser = UserModel(
        id: data['id'],
        email: email,
        fullName: data['fullName'] ?? 'User',
        role: data['role'] ?? 'User',
      );
      notifyListeners();
      
      if (context.mounted) {
         TopNotification.show(context: context, message: 'تم تسجيل الدخول بنجاح!', isSuccess: true);
      }
      return true;
    } else {
      if (context.mounted) {
        TopNotification.show(context: context, message: 'فشل الدخول: ${result['message']}', isSuccess: false);
      }
      return false;
    }
  }

  Future<bool> register(BuildContext context, String fullName, String email, String password, {String? phone}) async {
    _setLoading(true);
    final result = await _authService.register(
      fullName: fullName,
      email: email,
      phone: phone,
      password: password
    );
    _setLoading(false);

    if (result['success']) {
      if (context.mounted) {
        TopNotification.show(context: context, message: 'تم إنشاء الحساب بنجاح! يرجى إدخال رمز التفعيل.', isSuccess: true);
      }
      return true;
    } else {
      if (context.mounted) {
        TopNotification.show(context: context, message: 'فشل التسجيل: ${result['message']}', isSuccess: false);
      }
      return false;
    }
  }

  Future<bool> verifyOtp(BuildContext context, String email, String otpCode) async {
    _setLoading(true);
    final result = await _authService.verifyOtp(email: email, otpCode: otpCode);
    _setLoading(false);

    if (result['success']) {
      return true;
    } else {
      if (context.mounted) {
        TopNotification.show(context: context, message: 'فشل التحقق: ${result['message']}', isSuccess: false);
      }
      return false;
    }
  }

  Future<void> updateServerIp(BuildContext context, String ip) async {
    await ServerConfig.updateServerIp(ip);
    if (context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('تم تحديث عنوان السيرفر بنجاح')),
      );
    }
  }

  void _setLoading(bool value) {
    _isLoading = value;
    notifyListeners();
  }
}
