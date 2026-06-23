import 'package:flutter/material.dart';
import '../services/dashboard_service.dart';
import '../services/auth_service.dart';
import '../models/scan_model.dart';
import '../models/integration_model.dart';
import '../widgets/top_notification.dart';

class DashboardController extends ChangeNotifier {
  final DashboardService _dashboardService = DashboardService();
  
  bool _isLoading = false;
  bool _isScanning = false;
  bool _isConnecting = false;
  bool _isProfileLoading = false;
  
  bool _isLinked = false;
  String _linkedEmail = '';
  List<ScanModel> _scans = [];
  
  // Multi-integration support (typed)
  List<IntegrationModel> _integrations = [];

  // Currently selected integration for inbox isolation
  int? _selectedIntegrationId; // null = all accounts

  // User profile data
  Map<String, dynamic>? _userProfile;

  bool get isLoading => _isLoading;
  bool get isScanning => _isScanning;
  bool get isConnecting => _isConnecting;
  bool get isProfileLoading => _isProfileLoading;
  bool get isLinked => _isLinked;
  String get linkedEmail => _linkedEmail;
  List<ScanModel> get scans => _scans;
  List<IntegrationModel> get integrations => _integrations;
  int get integrationCount => _integrations.length;
  int? get selectedIntegrationId => _selectedIntegrationId;
  Map<String, dynamic>? get userProfile => _userProfile;

  /// Sets the active integration filter and reloads the dashboard scans.
  void setSelectedIntegration(int? id) {
    if (_selectedIntegrationId == id) return; // No-op if same
    _selectedIntegrationId = id;
    loadDashboardData();
  }

  Future<void> loadDashboardData() async {
    _isLoading = true;
    notifyListeners();
    
    final status = await _dashboardService.getIntegrationStatus();
    final rawScans = await _dashboardService.getRecentScans(
      integrationId: _selectedIntegrationId,
    );
    
    _isLinked = status['isLinked'] ?? false;
    _linkedEmail = status['email'] ?? '';
    
    // Parse integrations array with typed model
    if (status['integrations'] != null && status['integrations'] is List) {
      _integrations = (status['integrations'] as List)
          .map((i) => IntegrationModel.fromJson(Map<String, dynamic>.from(i)))
          .toList();
    } else {
      _integrations = [];
    }

    // Update linked status based on parsed integrations
    if (_integrations.isNotEmpty) {
      _isLinked = true;
    }
    
    try {
      _scans = rawScans.map((data) => ScanModel.fromJson(data as Map<String, dynamic>)).toList();
    } catch (e) {
      debugPrint('Error parsing scans: $e');
      _scans = [];
    }

    _isLoading = false;
    notifyListeners();
  }

  /// Loads the full user profile (name, email, phone, integrations).
  Future<void> loadUserProfile() async {
    _isProfileLoading = true;
    notifyListeners();

    _userProfile = await _dashboardService.getUserProfile();

    // Also update integrations from profile data for consistency
    if (_userProfile != null &&
        _userProfile!['integrations'] != null &&
        _userProfile!['integrations'] is List) {
      _integrations = (_userProfile!['integrations'] as List)
          .map((i) => IntegrationModel.fromJson(Map<String, dynamic>.from(i)))
          .toList();
    }

    _isProfileLoading = false;
    notifyListeners();
  }

  /// Logs the user out by clearing the session token.
  Future<void> logout() async {
    final authService = AuthService();
    await authService.logout();
  }

  Future<ScanModel?> performManualScan(BuildContext context, String email) async {
    if (email.trim().isEmpty) return null;

    _isScanning = true;
    notifyListeners();
    
    final result = await _dashboardService.analyzeEmail(email);
    
    _isScanning = false;
    notifyListeners();

    if (result['success']) {
      try {
        final scan = ScanModel.fromJson(result['data']);
        await loadDashboardData(); // Refresh historical list
        return scan;
      } catch (e) {
        debugPrint('Error parsing scan result: $e');
        if (context.mounted) {
           TopNotification.show(context: context, message: 'حدث خطأ أثناء قراءة النتائج', isSuccess: false);
        }
        return null;
      }
    } else {
      if (context.mounted) {
        TopNotification.show(context: context, message: result['message'], isSuccess: false);
      }
      return null;
    }
  }

  // ============================================
  // IMAP INTEGRATION ACTIONS (NEW — Web Parity)
  // ============================================

  /// Connects an IMAP account with optional manual host/port override.
  Future<bool> connectImap(
    BuildContext context, {
    required String email,
    required String password,
    String? imapHost,
    int? imapPort,
  }) async {
    _isConnecting = true;
    notifyListeners();

    final result = await _dashboardService.connectImap(
      email: email,
      password: password,
      imapHost: imapHost,
      imapPort: imapPort,
    );

    _isConnecting = false;
    notifyListeners();

    if (result['success']) {
      if (context.mounted) {
        TopNotification.show(context: context, message: result['message'], isSuccess: true);
      }
      await loadDashboardData(); // Refresh integration status
      return true;
    } else {
      if (context.mounted) {
        TopNotification.show(context: context, message: result['message'], isSuccess: false);
      }
      return false;
    }
  }

  /// Unlinks a connected integration by its ID.
  Future<bool> unlinkIntegration(BuildContext context, int integrationId) async {
    final result = await _dashboardService.unlinkIntegration(integrationId);

    if (result['success']) {
      if (context.mounted) {
        TopNotification.show(context: context, message: result['message'], isSuccess: true);
      }
      // If the unlinked integration was the selected one, reset filter
      if (_selectedIntegrationId == integrationId) {
        _selectedIntegrationId = null;
      }
      await loadDashboardData(); // Refresh
      return true;
    } else {
      if (context.mounted) {
        TopNotification.show(context: context, message: result['message'], isSuccess: false);
      }
      return false;
    }
  }
}

