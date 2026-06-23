import 'dart:ui';
import 'package:flutter/material.dart';
import '../models/scan_model.dart';

class ScanDetailsScreen extends StatelessWidget {
  final ScanModel scanData;
  
  const ScanDetailsScreen({super.key, required this.scanData});

  @override
  Widget build(BuildContext context) {
    final int score = scanData.score;
    final String verdict = scanData.verdict;
    final String email = scanData.email;
    final List<dynamic> reasons = scanData.reasons;
    final List<dynamic> recommendations = scanData.recommendations;
    final Map<String, dynamic> osint = scanData.osintData;

    // Additional Forensic Data
    final String provider = scanData.provider;
    final String country = scanData.country;
    final bool hasSpfRecord = scanData.hasSpfRecord;
    final bool hasDmarcRecord = scanData.hasDmarcRecord;
    final String plainTextBody = scanData.plainTextBody;
    final List<String> phishingKeywordsFound = scanData.phishingKeywordsFound;
    final List<String> maliciousUrls = scanData.maliciousUrls;
    final List<String> extractedUrls = scanData.extractedUrls;
    final List<String> attachmentNames = scanData.attachmentNames;
    final int phishingKeywordsCount = scanData.phishingKeywordsCount;
    final int maliciousLinksCount = scanData.maliciousLinksCount;
    final bool hasExecutableAttachments = scanData.hasExecutableAttachments;
    final bool hasSuspiciousAttachments = scanData.hasSuspiciousAttachments;

    // Advanced Algorithm Data
    final bool isTyposquatSuspect = scanData.isTyposquatSuspect;
    final String? typosquatMatchedDomain = scanData.typosquatMatchedDomain;
    final int typosquatDistance = scanData.typosquatDistance;
    final List<String> highEntropyUrls = scanData.highEntropyUrls;
    final List<String> highEntropyAttachments = scanData.highEntropyAttachments;
    final bool yaraMatched = scanData.yaraMatched;
    final List<String> yaraMatchedRules = scanData.yaraMatchedRules;

    Color riskColor;
    String riskTitle;

    if (verdict == 'SAFE' || score >= 70) {
      riskColor = Colors.tealAccent;
      riskTitle = 'آمن';
    } else if (verdict == 'HIGH' || verdict == 'RISK' || score < 40) {
      riskColor = Colors.redAccent;
      riskTitle = 'خطير جداً';
    } else {
      riskColor = Colors.orangeAccent;
      riskTitle = 'مشبوه (حذر)';
    }

    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        title: const Text('تفاصيل الفحص', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.transparent,
        elevation: 0,
        centerTitle: true,
      ),
      body: Stack(
        children: [
          // Dark sleek background
          Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [Color(0xFF0F172A), Color(0xFF1E1E2C)], // Very dark blue/purple tones
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
            ),
          ),
          // Decorative glowing circles
          Positioned(
            top: MediaQuery.of(context).size.height * 0.1,
            right: -50,
            child: Container(
              width: 300,
              height: 300,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: riskColor.withOpacity(0.15),
                boxShadow: [
                  BoxShadow(color: riskColor.withOpacity(0.2), blurRadius: 150, spreadRadius: 50),
                ],
              ),
            ),
          ),
          
          SafeArea(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                   // Risk Gauge & Score Card
                   _buildGlassCard(
                     child: Column(
                       children: [
                         Text(
                           email,
                           style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                           textAlign: TextAlign.center,
                         ),
                         const SizedBox(height: 24),
                         Stack(
                           alignment: Alignment.center,
                           children: [
                             SizedBox(
                               width: 150,
                               height: 150,
                               child: CircularProgressIndicator(
                                 value: score / 100,
                                 strokeWidth: 12,
                                 color: riskColor,
                                 backgroundColor: Colors.white.withOpacity(0.1),
                               ),
                             ),
                             Column(
                               mainAxisSize: MainAxisSize.min,
                               children: [
                                 Text(
                                   '$score%',
                                   style: TextStyle(
                                     fontSize: 36,
                                     fontWeight: FontWeight.bold,
                                     color: riskColor,
                                   ),
                                 ),
                                 Text(
                                   riskTitle,
                                   style: const TextStyle(color: Colors.white70, fontSize: 16),
                                 ),
                               ],
                             ),
                           ],
                         ),
                       ],
                     ),
                     borderColor: riskColor.withOpacity(0.5),
                   ),

                   const SizedBox(height: 24),

                   // Domain Authentication & Provider
                   _buildSectionTitle('تحليل المصدر', Icons.dns),
                   const SizedBox(height: 16),
                   _buildGlassCard(
                     child: Column(
                       crossAxisAlignment: CrossAxisAlignment.start,
                       children: [
                         _buildOsintItem("المزود (Provider)", provider),
                         _buildOsintItem("الدولة (Country)", country),
                         _buildOsintItem("سجل SPF", hasSpfRecord ? "مدعوم (Pass)" : "مفقود (Fail)"),
                         _buildOsintItem("سجل DMARC", hasDmarcRecord ? "مدعوم (Pass)" : "مفقود (Fail)"),
                       ],
                     ),
                     borderColor: (hasSpfRecord && hasDmarcRecord) ? Colors.tealAccent.withOpacity(0.5) : Colors.orangeAccent.withOpacity(0.5)
                   ),
                   const SizedBox(height: 24),

                   // ============================================
                   // ADVANCED ALGORITHM CARDS (NEW - Web Parity)
                   // ============================================
                   _buildSectionTitle('تحليل الخوارزميات المتقدمة', Icons.psychology),
                   const SizedBox(height: 16),
                   _buildAlgorithmCardsGrid(
                     isTyposquatSuspect: isTyposquatSuspect,
                     typosquatMatchedDomain: typosquatMatchedDomain,
                     typosquatDistance: typosquatDistance,
                     highEntropyUrls: highEntropyUrls,
                     highEntropyAttachments: highEntropyAttachments,
                     yaraMatched: yaraMatched,
                     yaraMatchedRules: yaraMatchedRules,
                   ),
                   const SizedBox(height: 24),

                   // Reasons Section (Why is it risky / safe?)
                   if (reasons.isNotEmpty) ...[
                     _buildSectionTitle('أسباب التقييم', Icons.analytics),
                     const SizedBox(height: 16),
                     _buildGlassCard(
                       child: Column(
                         crossAxisAlignment: CrossAxisAlignment.start,
                         children: reasons.map((r) => _buildListItem(r.toString(), Icons.warning_amber_rounded, Colors.orangeAccent)).toList(),
                       ),
                     ),
                     const SizedBox(height: 24),
                   ],

                   // Attachment Forensics
                   if (attachmentNames.isNotEmpty) ...[
                     _buildSectionTitle('المرفقات (${attachmentNames.length})', Icons.attach_file),
                     const SizedBox(height: 16),
                     _buildGlassCard(
                       child: Column(
                         crossAxisAlignment: CrossAxisAlignment.start,
                         children: [
                           if (hasExecutableAttachments)
                             _buildListItem("تحذير: يحتوي البريد على ملفات تنفيذية قابلة للتشغيل!", Icons.warning, Colors.redAccent),
                           if (hasSuspiciousAttachments && !hasExecutableAttachments)
                             _buildListItem("تنبيه: مرفقات مشبوهة تحتاج للتعامل بحذر.", Icons.warning_amber_rounded, Colors.orangeAccent),
                           const SizedBox(height: 8),
                           ...attachmentNames.map((att) => _buildOsintItem("ملف:", att)),
                         ],
                       ),
                       borderColor: hasExecutableAttachments ? Colors.redAccent.withOpacity(0.5) : null
                     ),
                     const SizedBox(height: 24),
                   ],

                   // URL Intelligence (with entropy highlighting)
                   if (extractedUrls.isNotEmpty) ...[
                     _buildSectionTitle('الروابط الخبيثة ($maliciousLinksCount خطر / ${extractedUrls.length} إجمالي)', Icons.link),
                     const SizedBox(height: 16),
                     _buildGlassCard(
                       child: Column(
                         crossAxisAlignment: CrossAxisAlignment.start,
                         children: extractedUrls.map((url) {
                           bool isMalicious = maliciousUrls.contains(url);
                           bool isHighEntropy = highEntropyUrls.contains(url);
                           
                           if (isMalicious) {
                             return _buildListItem(url, Icons.gpp_bad, Colors.redAccent);
                           } else if (isHighEntropy) {
                             return _buildListItem('$url (Entropy عالي)', Icons.bar_chart, Colors.orangeAccent);
                           } else {
                             return _buildListItem(url, Icons.link, Colors.tealAccent);
                           }
                         }).toList(),
                       ),
                       borderColor: maliciousLinksCount > 0 ? Colors.redAccent.withOpacity(0.5) : null
                     ),
                     const SizedBox(height: 24),
                   ],

                   // Snippet & NLP
                   if (plainTextBody.isNotEmpty) ...[
                     _buildSectionTitle('محتوى الرسالة (كلمات تصيد: $phishingKeywordsCount)', Icons.text_snippet),
                     const SizedBox(height: 16),
                     _buildGlassCard(
                       child: Column(
                         crossAxisAlignment: CrossAxisAlignment.stretch,
                         children: [
                           Text(
                             plainTextBody,
                             style: const TextStyle(color: Colors.white70, fontSize: 14, height: 1.5),
                             textDirection: TextDirection.rtl,
                           ),
                           if (phishingKeywordsFound.isNotEmpty) ...[
                             const Padding(
                               padding: EdgeInsets.symmetric(vertical: 12),
                               child: Divider(color: Colors.white24, height: 1),
                             ),
                             Text(
                               "الكلمات المشبوهة المكتشفة: ${phishingKeywordsFound.join('، ')}",
                               style: const TextStyle(color: Colors.orangeAccent, fontSize: 13, fontWeight: FontWeight.bold),
                             )
                           ]
                         ],
                       ),
                       borderColor: phishingKeywordsCount > 0 ? Colors.orangeAccent.withOpacity(0.5) : null
                     ),
                     const SizedBox(height: 24),
                   ],

                   // OSINT Data Section
                   if (osint.isNotEmpty) ...[
                     _buildSectionTitle('بيانات الاستخبارات المتوفرة (OSINT)', Icons.travel_explore),
                     const SizedBox(height: 16),
                     _buildGlassCard(
                       child: Column(
                         crossAxisAlignment: CrossAxisAlignment.start,
                         children: osint.entries.map((e) => _buildOsintItem(e.key, e.value.toString())).toList(),
                       ),
                     ),
                     const SizedBox(height: 24),
                   ],

                   // Recommendations Section
                   if (recommendations.isNotEmpty) ...[
                     _buildSectionTitle('التوصيات الأمنية', Icons.security),
                     const SizedBox(height: 16),
                     _buildGlassCard(
                       child: Column(
                         crossAxisAlignment: CrossAxisAlignment.start,
                         children: recommendations.map((r) => _buildListItem(r.toString(), Icons.check_circle_outline, Colors.cyanAccent)).toList(),
                       ),
                       borderColor: Colors.cyanAccent.withOpacity(0.3),
                     ),
                   ],
                   
                   const SizedBox(height: 48), // Bottom padding
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  // ============================================
  // ADVANCED ALGORITHM CARDS GRID
  // ============================================
  Widget _buildAlgorithmCardsGrid({
    required bool isTyposquatSuspect,
    String? typosquatMatchedDomain,
    required int typosquatDistance,
    required List<String> highEntropyUrls,
    required List<String> highEntropyAttachments,
    required bool yaraMatched,
    required List<String> yaraMatchedRules,
  }) {
    final int entropyTotal = highEntropyUrls.length + highEntropyAttachments.length;

    return Column(
      children: [
        // Row 1: Typosquatting + Entropy
        Row(
          children: [
            Expanded(
              child: _buildAlgoMiniCard(
                icon: Icons.text_rotation_angledown,
                title: 'انتحال النطاق',
                subtitle: 'Typosquatting',
                isAlert: isTyposquatSuspect,
                alertColor: Colors.redAccent,
                safeColor: Colors.tealAccent,
                alertValue: isTyposquatSuspect
                    ? '⚠️ مشبوه'
                    : '✅ سليم',
                alertDetail: isTyposquatSuspect
                    ? 'مشابه لـ ${typosquatMatchedDomain ?? '?'} (فرق $typosquatDistance حرف)'
                    : 'لا يوجد تشابه مشبوه',
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildAlgoMiniCard(
                icon: Icons.bar_chart,
                title: 'تحليل العشوائية',
                subtitle: 'Entropy',
                isAlert: entropyTotal > 0,
                alertColor: Colors.orangeAccent,
                safeColor: Colors.tealAccent,
                alertValue: entropyTotal > 0
                    ? '$entropyTotal عنصر مشبوه'
                    : '✅ سليم',
                alertDetail: entropyTotal > 0
                    ? '${highEntropyUrls.length > 0 ? '${highEntropyUrls.length} رابط' : ''} ${highEntropyAttachments.length > 0 ? '${highEntropyAttachments.length} مرفق' : ''} بنمط عشوائي'
                    : 'لا توجد أنماط عشوائية مشبوهة',
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        // Row 2: YARA (full width — critical alert)
        _YaraForensicsCard(
          yaraMatched: yaraMatched,
          yaraMatchedRules: yaraMatchedRules,
        ),
      ],
    );
  }

  Widget _buildAlgoMiniCard({
    required IconData icon,
    required String title,
    required String subtitle,
    required bool isAlert,
    required Color alertColor,
    required Color safeColor,
    required String alertValue,
    required String alertDetail,
  }) {
    final Color borderColor = isAlert ? alertColor.withOpacity(0.6) : Colors.white.withOpacity(0.1);
    final Color valueColor = isAlert ? alertColor : safeColor;

    return ClipRRect(
      borderRadius: BorderRadius.circular(20),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
        child: Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: isAlert ? alertColor.withOpacity(0.06) : Colors.white.withOpacity(0.04),
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: borderColor, width: 1.5),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Icon(icon, color: valueColor, size: 20),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      title,
                      style: const TextStyle(color: Colors.white70, fontSize: 12, fontWeight: FontWeight.w600),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
              Text(
                subtitle,
                style: TextStyle(color: Colors.white.withOpacity(0.3), fontSize: 10),
              ),
              const SizedBox(height: 10),
              Text(
                alertValue,
                style: TextStyle(color: valueColor, fontSize: 16, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 4),
              Text(
                alertDetail,
                style: const TextStyle(color: Colors.white54, fontSize: 11),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSectionTitle(String title, IconData icon) {
    return Row(
      children: [
        Icon(icon, color: Colors.cyanAccent, size: 28),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            title,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 20,
              fontWeight: FontWeight.bold,
            ),
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }

  Widget _buildGlassCard({required Widget child, Color? borderColor}) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(24),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
        child: Container(
          padding: const EdgeInsets.all(24),
          decoration: BoxDecoration(
            color: Colors.white.withOpacity(0.05),
            borderRadius: BorderRadius.circular(24),
            border: Border.all(
              color: borderColor ?? Colors.white.withOpacity(0.1),
              width: 1.5,
            ),
          ),
          child: child,
        ),
      ),
    );
  }

  Widget _buildListItem(String text, IconData icon, Color iconColor) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: iconColor, size: 20),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              text,
              style: const TextStyle(color: Colors.white, fontSize: 16, height: 1.5),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildOsintItem(String key, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            flex: 2,
            child: Text(
              key,
              style: const TextStyle(color: Colors.white54, fontSize: 15),
            ),
          ),
          Expanded(
            flex: 3,
            child: Text(
              value,
              style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w500),
              textAlign: TextAlign.end,
            ),
          ),
        ],
      ),
    );
  }
}

// ============================================
// YARA FORENSICS CARD (Animated Pulsing Alert)
// ============================================
class _YaraForensicsCard extends StatefulWidget {
  final bool yaraMatched;
  final List<String> yaraMatchedRules;

  const _YaraForensicsCard({
    required this.yaraMatched,
    required this.yaraMatchedRules,
  });

  @override
  State<_YaraForensicsCard> createState() => _YaraForensicsCardState();
}

class _YaraForensicsCardState extends State<_YaraForensicsCard>
    with SingleTickerProviderStateMixin {
  late AnimationController _pulseController;
  late Animation<double> _pulseAnimation;

  @override
  void initState() {
    super.initState();
    _pulseController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1500),
    );
    _pulseAnimation = Tween<double>(begin: 0.3, end: 0.8).animate(
      CurvedAnimation(parent: _pulseController, curve: Curves.easeInOut),
    );
    if (widget.yaraMatched) {
      _pulseController.repeat(reverse: true);
    }
  }

  @override
  void dispose() {
    _pulseController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final bool isAlert = widget.yaraMatched;
    final Color borderColor = isAlert ? Colors.redAccent : Colors.white.withOpacity(0.1);
    final Color valueColor = isAlert ? Colors.redAccent : Colors.tealAccent;

    Widget card = ClipRRect(
      borderRadius: BorderRadius.circular(20),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
        child: Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: isAlert ? Colors.redAccent.withOpacity(0.08) : Colors.white.withOpacity(0.04),
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: borderColor.withOpacity(0.6), width: 1.5),
          ),
          child: Row(
            children: [
              // Icon section
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: valueColor.withOpacity(0.15),
                ),
                child: Icon(
                  isAlert ? Icons.gpp_bad : Icons.verified_user,
                  color: valueColor,
                  size: 28,
                ),
              ),
              const SizedBox(width: 16),
              // Text section
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Row(
                      children: [
                        Text(
                          'فحص YARA',
                          style: TextStyle(
                            color: Colors.white.withOpacity(0.7),
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(width: 6),
                        Text(
                          '(توقيعات خبيثة)',
                          style: TextStyle(
                            color: Colors.white.withOpacity(0.3),
                            fontSize: 11,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      isAlert ? '🚨 كشف تهديد!' : '✅ نظيف',
                      style: TextStyle(
                        color: valueColor,
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    if (isAlert && widget.yaraMatchedRules.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Text(
                        'قواعد: ${widget.yaraMatchedRules.join(', ')}',
                        style: const TextStyle(color: Colors.redAccent, fontSize: 12),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ] else if (!isAlert) ...[
                      const SizedBox(height: 4),
                      const Text(
                        'لم يتم رصد توقيعات برمجيات خبيثة',
                        style: TextStyle(color: Colors.white54, fontSize: 12),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );

    // Wrap with pulsing glow animation only when YARA matched
    if (isAlert) {
      return AnimatedBuilder(
        animation: _pulseAnimation,
        builder: (context, child) {
          return Container(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(20),
              boxShadow: [
                BoxShadow(
                  color: Colors.redAccent.withOpacity(_pulseAnimation.value * 0.4),
                  blurRadius: 20,
                  spreadRadius: 2,
                ),
              ],
            ),
            child: child,
          );
        },
        child: card,
      );
    }

    return card;
  }
}
