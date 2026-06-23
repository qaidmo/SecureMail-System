import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../controllers/dashboard_controller.dart';

class ConnectAccountScreen extends StatefulWidget {
  const ConnectAccountScreen({super.key});

  @override
  State<ConnectAccountScreen> createState() => _ConnectAccountScreenState();
}

class _ConnectAccountScreenState extends State<ConnectAccountScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _hostController = TextEditingController();
  final _portController = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  
  bool _showAdvanced = false;
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _hostController.dispose();
    _portController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        title: const Text('ربط حساب بريد', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.transparent,
        elevation: 0,
        centerTitle: true,
      ),
      body: Stack(
        children: [
          // Background
          Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [Color(0xFF0F172A), Color(0xFF1E1E2C), Color(0xFF0F172A)],
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
              ),
            ),
          ),
          // Decorative glow
          Positioned(
            top: -80,
            left: -80,
            child: Container(
              width: 250,
              height: 250,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.cyanAccent.withOpacity(0.08),
                boxShadow: [
                  BoxShadow(color: Colors.cyanAccent.withOpacity(0.1), blurRadius: 120, spreadRadius: 40),
                ],
              ),
            ),
          ),
          SafeArea(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Directionality(
                textDirection: TextDirection.rtl,
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const SizedBox(height: 20),
                      // Header
                      _buildGlassCard(
                        child: Column(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(16),
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                color: Colors.cyanAccent.withOpacity(0.12),
                              ),
                              child: const Icon(Icons.link, color: Colors.cyanAccent, size: 36),
                            ),
                            const SizedBox(height: 16),
                            const Text(
                              'اتصال عبر IMAP',
                              style: TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.bold),
                              textAlign: TextAlign.center,
                            ),
                            const SizedBox(height: 8),
                            Text(
                              'أدخل بريدك الإلكتروني وكلمة مرور التطبيق لربط حسابك وبدء الفحص التلقائي.',
                              style: TextStyle(color: Colors.white.withOpacity(0.6), fontSize: 14, height: 1.5),
                              textAlign: TextAlign.center,
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 24),

                      // Email Field
                      _buildGlassCard(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            _buildLabel('البريد الإلكتروني'),
                            const SizedBox(height: 8),
                            TextFormField(
                              controller: _emailController,
                              keyboardType: TextInputType.emailAddress,
                              textDirection: TextDirection.ltr,
                              style: const TextStyle(color: Colors.white, fontSize: 15),
                              validator: (val) {
                                if (val == null || val.trim().isEmpty) return 'البريد مطلوب';
                                if (!val.contains('@')) return 'بريد إلكتروني غير صالح';
                                return null;
                              },
                              decoration: _inputDecoration('user@company.com', Icons.email),
                            ),
                            const SizedBox(height: 20),

                            // Password Field
                            _buildLabel('كلمة مرور التطبيق (IMAP)'),
                            const SizedBox(height: 8),
                            TextFormField(
                              controller: _passwordController,
                              obscureText: _obscurePassword,
                              textDirection: TextDirection.ltr,
                              style: const TextStyle(color: Colors.white, fontSize: 15),
                              validator: (val) {
                                if (val == null || val.trim().isEmpty) return 'كلمة المرور مطلوبة';
                                return null;
                              },
                              decoration: _inputDecoration('••••••••', Icons.lock).copyWith(
                                suffixIcon: IconButton(
                                  icon: Icon(
                                    _obscurePassword ? Icons.visibility_off : Icons.visibility,
                                    color: Colors.white38,
                                    size: 20,
                                  ),
                                  onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // Advanced Settings Toggle
                      GestureDetector(
                        onTap: () => setState(() => _showAdvanced = !_showAdvanced),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(vertical: 8),
                          child: Row(
                            children: [
                              Icon(
                                _showAdvanced ? Icons.expand_less : Icons.expand_more,
                                color: Colors.cyanAccent,
                                size: 22,
                              ),
                              const SizedBox(width: 8),
                              const Text(
                                'إعدادات متقدمة (اختياري)',
                                style: TextStyle(color: Colors.cyanAccent, fontSize: 14, fontWeight: FontWeight.w600),
                              ),
                            ],
                          ),
                        ),
                      ),

                      // Advanced Settings Panel
                      AnimatedCrossFade(
                        firstChild: const SizedBox.shrink(),
                        secondChild: _buildGlassCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Icon(Icons.settings_ethernet, color: Colors.cyanAccent.withOpacity(0.7), size: 18),
                                  const SizedBox(width: 8),
                                  const Text(
                                    'إعدادات IMAP اليدوية',
                                    style: TextStyle(color: Colors.white70, fontSize: 14, fontWeight: FontWeight.w600),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 16),
                              Row(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Expanded(
                                    flex: 3,
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        _buildLabel('خادم IMAP'),
                                        const SizedBox(height: 8),
                                        TextFormField(
                                          controller: _hostController,
                                          textDirection: TextDirection.ltr,
                                          style: const TextStyle(color: Colors.white, fontSize: 14),
                                          decoration: _inputDecoration('imap.zoho.com', Icons.dns),
                                        ),
                                      ],
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    flex: 2,
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        _buildLabel('المنفذ'),
                                        const SizedBox(height: 8),
                                        TextFormField(
                                          controller: _portController,
                                          keyboardType: TextInputType.number,
                                          textDirection: TextDirection.ltr,
                                          style: const TextStyle(color: Colors.white, fontSize: 14),
                                          decoration: _inputDecoration('993', Icons.numbers),
                                        ),
                                      ],
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 12),
                              Text(
                                '💡 اتركها فارغة للكشف التلقائي. استخدمها فقط إذا فشل الاتصال التلقائي.',
                                style: TextStyle(color: Colors.white.withOpacity(0.4), fontSize: 12, height: 1.4),
                              ),
                            ],
                          ),
                        ),
                        crossFadeState: _showAdvanced ? CrossFadeState.showSecond : CrossFadeState.showFirst,
                        duration: const Duration(milliseconds: 300),
                      ),
                      const SizedBox(height: 24),

                      // Connect Button
                      Consumer<DashboardController>(
                        builder: (context, dashboard, _) {
                          return Container(
                            height: 55,
                            decoration: BoxDecoration(
                              borderRadius: BorderRadius.circular(16),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.cyanAccent.withOpacity(0.3),
                                  blurRadius: 15,
                                  spreadRadius: 1,
                                ),
                              ],
                            ),
                            child: ElevatedButton(
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.cyanAccent.shade700,
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(16),
                                ),
                                elevation: 0,
                              ),
                              onPressed: dashboard.isConnecting
                                  ? null
                                  : () => _handleConnect(context, dashboard),
                              child: dashboard.isConnecting
                                  ? const SizedBox(
                                      width: 24,
                                      height: 24,
                                      child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                                    )
                                  : const Row(
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      children: [
                                        Icon(Icons.link, color: Colors.white, size: 22),
                                        SizedBox(width: 10),
                                        Text(
                                          'اتصال عبر IMAP',
                                          style: TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold),
                                        ),
                                      ],
                                    ),
                            ),
                          );
                        },
                      ),
                      const SizedBox(height: 16),

                      // Tip footer
                      ClipRRect(
                        borderRadius: BorderRadius.circular(12),
                        child: BackdropFilter(
                          filter: ImageFilter.blur(sigmaX: 5, sigmaY: 5),
                          child: Container(
                            padding: const EdgeInsets.all(14),
                            decoration: BoxDecoration(
                              color: Colors.white.withOpacity(0.03),
                              borderRadius: BorderRadius.circular(12),
                              border: Border.all(color: Colors.white.withOpacity(0.06)),
                            ),
                            child: Row(
                              children: [
                                Icon(Icons.lightbulb, color: Colors.amber.withOpacity(0.7), size: 20),
                                const SizedBox(width: 10),
                                const Expanded(
                                  child: Text(
                                    'لحسابات Gmail/Outlook مع المصادقة الثنائية، استخدم "كلمة مرور التطبيقات" (App Password).',
                                    style: TextStyle(color: Colors.white54, fontSize: 12, height: 1.4),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),

                      const SizedBox(height: 24),

                      // Linked Accounts Section
                      _buildLinkedAccountsSection(),

                      const SizedBox(height: 48),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _handleConnect(BuildContext context, DashboardController dashboard) async {
    if (!_formKey.currentState!.validate()) return;

    final email = _emailController.text.trim();
    final password = _passwordController.text;
    final host = _hostController.text.trim();
    final portText = _portController.text.trim();
    int? port;

    if (portText.isNotEmpty) {
      port = int.tryParse(portText);
      if (port == null || port < 1 || port > 65535) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('رقم المنفذ غير صالح (يجب أن يكون بين 1 و 65535)', textAlign: TextAlign.right)),
        );
        return;
      }
    }

    final success = await dashboard.connectImap(
      context,
      email: email,
      password: password,
      imapHost: host.isNotEmpty ? host : null,
      imapPort: port,
    );

    if (success && mounted) {
      _emailController.clear();
      _passwordController.clear();
      _hostController.clear();
      _portController.clear();
      setState(() => _showAdvanced = false);
    }
  }

  Widget _buildLinkedAccountsSection() {
    return Consumer<DashboardController>(
      builder: (context, dashboard, _) {
        if (dashboard.integrations.isEmpty) {
          return const SizedBox.shrink();
        }

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.manage_accounts, color: Colors.cyanAccent, size: 24),
                SizedBox(width: 8),
                Text(
                  'الحسابات المربوطة',
                  style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ...dashboard.integrations.map((integration) {
              final email = integration.providerAccountEmail;
              final provider = integration.providerLabel;
              final id = integration.id;
              final icon = integration.providerIcon;

              return Container(
                margin: const EdgeInsets.only(bottom: 10),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(16),
                  child: BackdropFilter(
                    filter: ImageFilter.blur(sigmaX: 5, sigmaY: 5),
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                      decoration: BoxDecoration(
                        color: Colors.white.withOpacity(0.04),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: Colors.white.withOpacity(0.08)),
                      ),
                      child: Row(
                        children: [
                          Text(icon, style: const TextStyle(fontSize: 22)),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  email,
                                  style: const TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.w600),
                                  overflow: TextOverflow.ellipsis,
                                ),
                                Text(
                                  provider,
                                  style: const TextStyle(color: Colors.white38, fontSize: 12),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(width: 8),
                          IconButton(
                            icon: const Icon(Icons.link_off, color: Colors.redAccent, size: 22),
                            tooltip: 'إلغاء الربط',
                            onPressed: () => _confirmUnlink(context, dashboard, id, email),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              );
            }),
          ],
        );
      },
    );
  }

  void _confirmUnlink(BuildContext context, DashboardController dashboard, int id, String email) {
    showDialog(
      context: context,
      builder: (ctx) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          backgroundColor: const Color(0xFF1E1E2C),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          title: const Text('إلغاء ربط الحساب', style: TextStyle(color: Colors.white)),
          content: Text(
            'هل أنت متأكد من إلغاء ربط $email؟',
            style: const TextStyle(color: Colors.white70),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('إلغاء', style: TextStyle(color: Colors.white54)),
            ),
            TextButton(
              onPressed: () {
                Navigator.pop(ctx);
                dashboard.unlinkIntegration(context, id);
              },
              child: const Text('نعم، إلغاء الربط', style: TextStyle(color: Colors.redAccent, fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildGlassCard({required Widget child}) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(20),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
        child: Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: Colors.white.withOpacity(0.05),
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: Colors.white.withOpacity(0.1), width: 1),
          ),
          child: child,
        ),
      ),
    );
  }

  Widget _buildLabel(String text) {
    return Text(
      text,
      style: TextStyle(color: Colors.white.withOpacity(0.6), fontSize: 13, fontWeight: FontWeight.w500),
    );
  }

  InputDecoration _inputDecoration(String hint, IconData icon) {
    return InputDecoration(
      hintText: hint,
      hintStyle: const TextStyle(color: Colors.white30, fontSize: 14),
      prefixIcon: Icon(icon, color: Colors.cyanAccent.withOpacity(0.6), size: 20),
      filled: true,
      fillColor: Colors.white.withOpacity(0.04),
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide(color: Colors.white.withOpacity(0.1)),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide(color: Colors.white.withOpacity(0.1)),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: Colors.cyanAccent),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: Colors.redAccent),
      ),
    );
  }
}
