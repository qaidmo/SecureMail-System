import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../controllers/dashboard_controller.dart';
import 'scan_details_screen.dart';

class ManualScanScreen extends StatefulWidget {
  const ManualScanScreen({super.key});

  @override
  State<ManualScanScreen> createState() => _ManualScanScreenState();
}

class _ManualScanScreenState extends State<ManualScanScreen> {
  final _emailController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('فحص إيميل يدوي', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF0F172A),
        elevation: 0,
        centerTitle: true,
      ),
      body: Stack(
        children: [
          // Gradient Background
          Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [Color(0xFF0F172A), Color(0xFF1E1E2C)],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
            ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(24.0),
              child: Directionality(
                textDirection: TextDirection.rtl,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                     const Text(
                       'أدخل البريد الإلكتروني الذي تشك فيه لفحصه بواسطة الذكاء الاصطناعي وبحث مصادر الاستخبارات المفتوحة.',
                       style: TextStyle(color: Colors.white70, fontSize: 16, height: 1.5),
                     ),
                     const SizedBox(height: 32),
                     TextField(
                        controller: _emailController,
                        style: const TextStyle(color: Colors.white),
                        keyboardType: TextInputType.emailAddress,
                        textDirection: TextDirection.ltr,
                        decoration: InputDecoration(
                          hintText: 'user@example.com',
                          hintStyle: const TextStyle(color: Colors.white54),
                          prefixIcon: const Icon(Icons.search, color: Colors.cyanAccent),
                          filled: true,
                          fillColor: Colors.white.withOpacity(0.05),
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(16),
                            borderSide: BorderSide(color: Colors.cyanAccent.withOpacity(0.3)),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(16),
                            borderSide: const BorderSide(color: Colors.white24),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(16),
                            borderSide: const BorderSide(color: Colors.cyanAccent),
                          ),
                        ),
                      ),
                      const SizedBox(height: 32),
                      Consumer<DashboardController>(
                        builder: (context, dashboard, _) {
                          return SizedBox(
                            width: double.infinity,
                            height: 55,
                            child: ElevatedButton(
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.cyanAccent.shade700,
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(16),
                                ),
                                elevation: 10,
                                shadowColor: Colors.cyanAccent.withOpacity(0.5),
                              ),
                              onPressed: dashboard.isScanning
                                  ? null
                                  : () async {
                                      final testEmail = _emailController.text.trim();
                                      if (testEmail.isEmpty) return;
  
                                      final scanResult = await dashboard.performManualScan(context, testEmail);
  
                                      if (mounted && scanResult != null) {
                                        Navigator.pushReplacement(
                                           context,
                                           PageRouteBuilder(
                                              pageBuilder: (_, __, ___) => ScanDetailsScreen(scanData: scanResult),
                                              transitionsBuilder: (_, a, __, c) => FadeTransition(opacity: a, child: c),
                                           ),
                                        );
                                      }
                                    },
                              child: dashboard.isScanning
                                  ? const CircularProgressIndicator(color: Colors.white)
                                  : const Text(
                                      'بدء الفحص الآن',
                                      style: TextStyle(fontSize: 18, color: Colors.white, fontWeight: FontWeight.bold),
                                    ),
                            ),
                          );
                        },
                      ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
