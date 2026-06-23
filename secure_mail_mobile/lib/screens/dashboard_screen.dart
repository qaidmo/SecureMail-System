import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../controllers/dashboard_controller.dart';
import '../models/scan_model.dart';
import '../models/integration_model.dart';
import '../services/dashboard_service.dart';
import 'scan_details_screen.dart';
import 'manual_scan_screen.dart';
import 'connect_account_screen.dart';
import 'profile_screen.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      Provider.of<DashboardController>(context, listen: false).loadDashboardData();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('لوحة التحكم', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF0F172A),
        elevation: 0,
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.person_outline, color: Colors.cyanAccent, size: 26),
            tooltip: 'الملف الشخصي',
            onPressed: () {
              Navigator.push(context, MaterialPageRoute(builder: (_) => const ProfileScreen()));
            },
          ),
          const SizedBox(width: 4),
        ],
      ),
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFF0F172A), Color(0xFF1E1E2C), Color(0xFF0F172A)],
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
          ),
        ),
        child: Column(
          children: [
          // ── Account Switcher Row ──
          Consumer<DashboardController>(
            builder: (context, dashboard, _) {
              if (dashboard.integrations.isEmpty) return const SizedBox.shrink();
              return _buildAccountSwitcherRow(dashboard);
            },
          ),
          // ── Main Content ──
          Expanded(child: Consumer<DashboardController>(
            builder: (context, dashboard, _) {
              if (dashboard.isLoading) {
                return const Center(child: CircularProgressIndicator(color: Colors.cyanAccent));
              }

              return RefreshIndicator(
                onRefresh: dashboard.loadDashboardData,
                color: Colors.cyanAccent,
                child: SingleChildScrollView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Directionality(
                      textDirection: TextDirection.rtl,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'ملخص الإحصائيات',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 20,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 16),
                          
                          // 4 Glassmorphism Cards Grid
                          _buildStatsGrid(dashboard.isLinked, dashboard.scans),

                          const SizedBox(height: 32),
                          
                          const Text(
                            'الفحوصات الأخيرة',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 20,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 16),
                          if (dashboard.scans.isEmpty)
                            const Center(
                              child: Padding(
                                padding: EdgeInsets.all(32.0),
                                child: Text(
                                  'لا توجد فحوصات سابقة.',
                                  style: TextStyle(color: Colors.white54),
                                ),
                              ),
                            )
                          else
                            ...dashboard.scans.map((scan) => _buildScanCard(scan)).toList(),
                            
                          const SizedBox(height: 80), // Fab space
                        ],
                      ),
                    ),
                  ),
                ),
              );
            },
          )),
          ],
        ),
      ),
      floatingActionButton: Container(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(30),
          boxShadow: [
            BoxShadow(
              color: Colors.cyanAccent.withOpacity(0.4),
              blurRadius: 15,
              spreadRadius: 2,
            )
          ],
        ),
        child: FloatingActionButton.extended(
          onPressed: () {
            Navigator.push(context, MaterialPageRoute(builder: (_) => const ManualScanScreen()));
          },
          backgroundColor: Colors.cyanAccent.shade700,
          icon: const Icon(Icons.radar, color: Colors.white),
          label: const Text(
            'فحص جديد',
            style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
          ),
        ),
      ),
    );
  }

  Widget _buildStatsGrid(bool isLinked, List<ScanModel> scans) {
    int safeCount = scans.where((s) => s.verdict == 'SAFE' || s.score >= 70).length;
    int riskCount = scans.length - safeCount;
    int total = scans.length;
    final int linkedCount = Provider.of<DashboardController>(context, listen: false).integrationCount;

    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _buildGlassCard(
                title: 'إجمالي الفحوصات',
                value: total.toString(),
                icon: Icons.analytics,
                color: Colors.cyanAccent,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: _buildGlassCard(
                title: 'رسائل آمنة',
                value: safeCount.toString(),
                icon: Icons.verified_user,
                color: Colors.tealAccent,
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: _buildGlassCard(
                title: 'تهديدات',
                value: riskCount.toString(),
                icon: Icons.gpp_bad,
                color: Colors.redAccent,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: GestureDetector(
                onTap: () {
                  Navigator.push(context, MaterialPageRoute(builder: (_) => const ConnectAccountScreen()));
                },
                child: _buildGlassCard(
                  title: 'حسابات مربوطة',
                  value: linkedCount > 0 ? '$linkedCount متصل' : 'غير متصل',
                  icon: linkedCount > 0 ? Icons.link : Icons.link_off,
                  color: linkedCount > 0 ? Colors.blueAccent : Colors.orangeAccent,
                ),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildGlassCard({required String title, required String value, required IconData icon, required Color color}) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(20),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
        child: Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: Colors.white.withOpacity(0.05),
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: Colors.white.withOpacity(0.1), width: 1),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: color, size: 36, shadows: [Shadow(color: color.withOpacity(0.5), blurRadius: 10)]),
              const SizedBox(height: 12),
              Text(
                value,
                style: const TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 4),
              Text(
                title,
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.white70, fontSize: 13),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildScanCard(ScanModel scan) {
    final verdict = scan.verdict;
    final email = scan.email;
    final date = scan.date != null ? '${scan.date!.day}/${scan.date!.month}/${scan.date!.year}' : '';
    final score = scan.score;

    Color statusColor;
    IconData statusIcon;

    if (verdict == 'SAFE') {
      statusColor = Colors.tealAccent;
      statusIcon = Icons.verified_user;
    } else if (verdict == 'HIGH' || verdict == 'RISK') {
      statusColor = Colors.redAccent;
      statusIcon = Icons.gpp_bad;
    } else {
      statusColor = Colors.orangeAccent;
      statusIcon = Icons.warning;
    }

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: Colors.white.withOpacity(0.04),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Colors.white.withOpacity(0.05), width: 1),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: 5, sigmaY: 5),
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              onTap: () async {
                if (scan.id == null) return;
                
                showDialog(
                  context: context,
                  barrierDismissible: false,
                  builder: (_) => const Center(child: CircularProgressIndicator(color: Colors.cyanAccent)),
                );
                
                final dashboardService = DashboardService();
                final deepData = await dashboardService.getScanDetails(scan.id.toString());
                
                Navigator.pop(context);
                
                if (deepData != null) {
                  final deepScan = ScanModel.fromJson(deepData);
                  Navigator.push(
                    context,
                    PageRouteBuilder(
                      pageBuilder: (_, __, ___) => ScanDetailsScreen(scanData: deepScan),
                      transitionsBuilder: (_, a, __, c) => FadeTransition(opacity: a, child: c),
                    ),
                  );
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('تعذر جلب تفاصيل الفحص', textAlign: TextAlign.right)),
                  );
                }
              },
              child: Directionality(
                textDirection: TextDirection.rtl,
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                           color: statusColor.withOpacity(0.1),
                           shape: BoxShape.circle,
                        ),
                        child: Icon(statusIcon, color: statusColor, size: 28),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                           crossAxisAlignment: CrossAxisAlignment.start,
                           children: [
                             Text(
                               email, 
                               style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 15), 
                               maxLines: 1, 
                               overflow: TextOverflow.ellipsis
                             ),
                             const SizedBox(height: 4),
                             Text(
                               '$verdict - الثقة: $score%', 
                               style: TextStyle(color: statusColor, fontSize: 12, fontWeight: FontWeight.bold)
                             ),
                           ],
                        )
                      ),
                      const SizedBox(width: 12),
                      Text(date, style: const TextStyle(color: Colors.white38, fontSize: 12)),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  // ── Account Switcher (Filter Row) ──
  Widget _buildAccountSwitcherRow(DashboardController dashboard) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
      decoration: BoxDecoration(
        color: Colors.white.withOpacity(0.03),
        border: Border(
          bottom: BorderSide(color: Colors.cyanAccent.withOpacity(0.08)),
        ),
      ),
      child: Directionality(
        textDirection: TextDirection.rtl,
        child: Row(
          children: [
            Icon(Icons.filter_alt_outlined,
                color: Colors.cyanAccent.withOpacity(0.6), size: 20),
            const SizedBox(width: 10),
            Text(
              'الحساب:',
              style: TextStyle(
                  color: Colors.white.withOpacity(0.5),
                  fontSize: 13,
                  fontWeight: FontWeight.w600),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                decoration: BoxDecoration(
                  color: Colors.white.withOpacity(0.04),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                      color: Colors.cyanAccent.withOpacity(0.15)),
                ),
                child: DropdownButtonHideUnderline(
                  child: DropdownButton<int?>(
                    value: dashboard.selectedIntegrationId,
                    isExpanded: true,
                    dropdownColor: const Color(0xFF1E1E2C),
                    style: const TextStyle(
                        color: Colors.white, fontSize: 13),
                    icon: Icon(Icons.keyboard_arrow_down,
                        color: Colors.cyanAccent.withOpacity(0.6),
                        size: 22),
                    items: [
                      const DropdownMenuItem<int?>(
                        value: null,
                        child: Text('📧  جميع الحسابات',
                            style: TextStyle(fontSize: 13)),
                      ),
                      ...dashboard.integrations.map((i) {
                        return DropdownMenuItem<int?>(
                          value: i.id,
                          child: Text(
                            '${i.providerIcon}  ${i.providerAccountEmail}',
                            style: const TextStyle(fontSize: 13),
                            overflow: TextOverflow.ellipsis,
                          ),
                        );
                      }),
                    ],
                    onChanged: (id) {
                      dashboard.setSelectedIntegration(id);
                    },
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
