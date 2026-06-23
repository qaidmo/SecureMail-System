class IntegrationModel {
  final int id;
  final String provider; // "GMAIL" or "IMAP"
  final String providerAccountEmail;
  final DateTime? createdAt;

  IntegrationModel({
    required this.id,
    required this.provider,
    required this.providerAccountEmail,
    this.createdAt,
  });

  factory IntegrationModel.fromJson(Map<String, dynamic> json) {
    return IntegrationModel(
      id: json['id'] as int? ?? 0,
      provider: json['provider'] ?? 'IMAP',
      providerAccountEmail: json['providerAccountEmail'] ?? 'غير معروف',
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'])
          : null,
    );
  }

  /// Returns a display-friendly provider label.
  String get providerLabel {
    switch (provider.toUpperCase()) {
      case 'GMAIL':
        return 'Google';
      case 'IMAP':
        return 'IMAP';
      default:
        return provider;
    }
  }

  /// Returns an emoji icon for the provider type.
  String get providerIcon {
    switch (provider.toUpperCase()) {
      case 'GMAIL':
        return '🔵';
      case 'IMAP':
        return '📬';
      default:
        return '📧';
    }
  }
}
