# 🛡️ SecureMail System

A multi-layered, real-time threat detection platform designed to analyze emails for phishing, malware, and typosquatting. Built with a robust C# .NET 8 backend, a cross-platform Flutter mobile application, and a responsive web dashboard.

## 🏗️ System Architecture

This repository contains the complete ecosystem divided into three main components:

*   **`SecureMailBackend/`**: The core API built with C# ASP.NET Core. It handles IMAP/Gmail polling, YARA rule execution, virus scanning (via VirusTotal API), and secure data storage.
*   **`secure_mail_mobile/`**: The cross-platform mobile application built with Flutter, providing real-time alerts, scan details, and dashboard analytics.
*   **`SecureMailWeb/`**: A lightweight web interface for quick dashboard access, user authentication, and system administration.

## ✨ Key Features

*   **Real-Time Email Polling**: Automated fetching and scanning of incoming emails via integration with external mail protocols.
*   **Advanced Threat Detection**: Utilizes YARA rules for signature-based malware detection, suspicious macro identification, and typosquatting algorithms.
*   **Secure Authentication**: Multi-layered access including OTP verification and secure session management.
*   **Data Protection**: AES-256 encryption implementation for securing sensitive payloads and communications.
*   **Cross-Platform Mobile UI**: Built with a focus on seamless user experience and offline-first architectural principles.

## 🛠️ Tech Stack

*   **Backend Framework**: C# .NET 8, Entity Framework Core.
*   **Mobile Development**: Flutter, Dart.
*   **Frontend**: HTML5, CSS3, Vanilla JavaScript.
*   **Security & Analysis Tools**: YARA, Custom Entropy Calculators, Advanced Address Checking.

## 🔒 Security Notice

This repository serves as a **Case Study** and structural architecture showcase. For strict security compliance, all sensitive configuration files, including `appsettings.json`, `firebase-adminsdk.json`, mobile `google-services.json`, and client environment variables, have been intentionally excluded via `.gitignore`. 

---
**Developed by QAID ALNJRY**  
*Full-Stack Mobile & Backend Developer* | 📍 Sana'a, Yemen
