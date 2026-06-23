class UserModel {
  final int? id;
  final String fullName;
  final String email;
  final String? phone;
  final String role;
  final DateTime? createdAt;
  final bool isEmailVerified;

  UserModel({
    this.id,
    required this.fullName,
    required this.email,
    this.phone,
    required this.role,
    this.createdAt,
    this.isEmailVerified = false,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'] as int?,
      fullName: json['fullName'] ?? '',
      email: json['email'] ?? '',
      phone: json['phone'],
      role: json['role'] ?? 'User',
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt']) : null,
      isEmailVerified: json['isEmailVerified'] ?? false,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'fullName': fullName,
      'email': email,
      'phone': phone,
      'role': role,
      'createdAt': createdAt?.toIso8601String(),
      'isEmailVerified': isEmailVerified,
    };
  }
}
