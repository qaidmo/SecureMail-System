class ScanModel {
  final int? id;
  final String email;
  final String verdict;
  final int score;
  final DateTime? date;
  
  // Forensic Details
  final String provider;
  final String country;
  final bool hasSpfRecord;
  final bool hasDmarcRecord;
  final String plainTextBody;
  
  final List<String> reasons;
  final List<String> recommendations;
  final List<String> phishingKeywordsFound;
  final List<String> maliciousUrls;
  final List<String> extractedUrls;
  final List<String> attachmentNames;
  
  final int phishingKeywordsCount;
  final int maliciousLinksCount;
  
  final bool hasExecutableAttachments;
  final bool hasSuspiciousAttachments;
  
  final Map<String, dynamic> osintData;

  // --- Advanced Algorithm Properties (Parity with Web Dashboard) ---
  
  // Typosquatting Detection
  final bool isTyposquatSuspect;
  final String? typosquatMatchedDomain;
  final int typosquatDistance;
  
  // Shannon Entropy Analysis
  final List<String> highEntropyUrls;
  final List<String> highEntropyAttachments;
  
  // YARA Forensics
  final bool yaraMatched;
  final List<String> yaraMatchedRules;

  ScanModel({
    this.id,
    required this.email,
    required this.verdict,
    required this.score,
    this.date,
    this.provider = 'مجهول',
    this.country = 'غير محدد',
    this.hasSpfRecord = false,
    this.hasDmarcRecord = false,
    this.plainTextBody = '',
    this.reasons = const [],
    this.recommendations = const [],
    this.phishingKeywordsFound = const [],
    this.maliciousUrls = const [],
    this.extractedUrls = const [],
    this.attachmentNames = const [],
    this.phishingKeywordsCount = 0,
    this.maliciousLinksCount = 0,
    this.hasExecutableAttachments = false,
    this.hasSuspiciousAttachments = false,
    this.osintData = const {},
    // Advanced algorithm defaults
    this.isTyposquatSuspect = false,
    this.typosquatMatchedDomain,
    this.typosquatDistance = 0,
    this.highEntropyUrls = const [],
    this.highEntropyAttachments = const [],
    this.yaraMatched = false,
    this.yaraMatchedRules = const [],
  });

  factory ScanModel.fromJson(Map<String, dynamic> json) {
    return ScanModel(
      id: json['id'] as int?,
      email: json['email'] ?? 'غير معروف',
      verdict: json['verdict'] ?? json['riskLevel'] ?? 'UNKNOWN',
      score: (json['score'] ?? json['trustScore'] ?? 0) as int,
      date: json['date'] != null ? DateTime.tryParse(json['date']) : null,
      
      provider: json['provider'] ?? 'مجهول',
      country: json['country'] ?? 'غير محدد',
      hasSpfRecord: json['hasSpfRecord'] ?? false,
      hasDmarcRecord: json['hasDmarcRecord'] ?? false,
      plainTextBody: json['plainTextBody'] ?? '',
      
      reasons: (json['reasons'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      recommendations: (json['recommendations'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      phishingKeywordsFound: (json['phishingKeywordsFound'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      maliciousUrls: (json['maliciousUrls'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      extractedUrls: (json['extractedUrls'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      attachmentNames: (json['attachmentNames'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      
      phishingKeywordsCount: json['phishingKeywordsCount'] as int? ?? 0,
      maliciousLinksCount: json['maliciousLinksCount'] as int? ?? 0,
      
      hasExecutableAttachments: json['hasExecutableAttachments'] ?? false,
      hasSuspiciousAttachments: json['hasSuspiciousAttachments'] ?? false,
      
      osintData: json['osintData'] as Map<String, dynamic>? ?? {},

      // --- Advanced Algorithm Parsing ---
      isTyposquatSuspect: json['isTyposquatSuspect'] ?? false,
      typosquatMatchedDomain: json['typosquatMatchedDomain'] as String?,
      typosquatDistance: json['typosquatDistance'] as int? ?? 0,
      highEntropyUrls: (json['highEntropyUrls'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      highEntropyAttachments: (json['highEntropyAttachments'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      yaraMatched: json['yaraMatched'] ?? false,
      yaraMatchedRules: (json['yaraMatchedRules'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'email': email,
      'verdict': verdict,
      'score': score,
      'date': date?.toIso8601String(),
      'provider': provider,
      'country': country,
      'hasSpfRecord': hasSpfRecord,
      'hasDmarcRecord': hasDmarcRecord,
      'plainTextBody': plainTextBody,
      'reasons': reasons,
      'recommendations': recommendations,
      'phishingKeywordsFound': phishingKeywordsFound,
      'maliciousUrls': maliciousUrls,
      'extractedUrls': extractedUrls,
      'attachmentNames': attachmentNames,
      'phishingKeywordsCount': phishingKeywordsCount,
      'maliciousLinksCount': maliciousLinksCount,
      'hasExecutableAttachments': hasExecutableAttachments,
      'hasSuspiciousAttachments': hasSuspiciousAttachments,
      'osintData': osintData,
      // Advanced algorithm fields
      'isTyposquatSuspect': isTyposquatSuspect,
      'typosquatMatchedDomain': typosquatMatchedDomain,
      'typosquatDistance': typosquatDistance,
      'highEntropyUrls': highEntropyUrls,
      'highEntropyAttachments': highEntropyAttachments,
      'yaraMatched': yaraMatched,
      'yaraMatchedRules': yaraMatchedRules,
    };
  }
}
