import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:provider/provider.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';

import 'core/server_config.dart';
import 'screens/splash_screen.dart';
import 'screens/scan_details_screen.dart';
import 'controllers/auth_controller.dart';
import 'controllers/dashboard_controller.dart';
import 'services/dashboard_service.dart';
import 'services/auth_service.dart';
import 'models/scan_model.dart';

final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

@pragma('vm:entry-point')
Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
  print("Handling a background push message: ${message.messageId}");
}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp();
  await ServerConfig.init(); 
  
  try {
    await FirebaseMessaging.instance.requestPermission(alert: true, badge: true, sound: true);
    print("FCM Token: ${await FirebaseMessaging.instance.getToken()}");
  } catch (e) {
    print("FCM Setup Error: $e");
  }
  
  FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);

  runApp(const SecureMailApp());
}

class SecureMailApp extends StatefulWidget {
  const SecureMailApp({super.key});

  @override
  State<SecureMailApp> createState() => _SecureMailAppState();
}

class _SecureMailAppState extends State<SecureMailApp> {
  @override
  void initState() {
    super.initState();
    _setupFirebaseMessaging();
  }

  Future<void> _setupFirebaseMessaging() async {
    final messaging = FirebaseMessaging.instance;
    await messaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
      provisional: false,
    );

    final token = await messaging.getToken();
    if (token != null) {
      final authService = AuthService();
      await authService.updateDeviceToken(token);
    }

    FirebaseMessaging.onMessage.listen((RemoteMessage message) {
      if (message.notification != null) {
        final context = navigatorKey.currentContext;
        if (context != null) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('${message.notification!.title}: ${message.notification!.body}', textAlign: TextAlign.right),
              backgroundColor: Colors.redAccent.withOpacity(0.9),
              action: SnackBarAction(
                label: 'التفاصيل',
                textColor: Colors.white,
                onPressed: () {
                  ScaffoldMessenger.of(context).hideCurrentSnackBar();
                  _handleMessage(message);
                },
              ),
              duration: const Duration(seconds: 5),
            ),
          );
        }
      }
    });

    FirebaseMessaging.onMessageOpenedApp.listen(_handleMessage);

    final initialMessage = await messaging.getInitialMessage();
    if (initialMessage != null) {
      Future.delayed(const Duration(milliseconds: 500), () {
        _handleMessage(initialMessage);
      });
    }
  }

  Future<void> _handleMessage(RemoteMessage message) async {
    final scanId = message.data['scanId'];
    if (scanId != null) {
      final context = navigatorKey.currentContext;
      if (context != null) {
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (_) => const Center(child: CircularProgressIndicator(color: Colors.cyanAccent)),
        );

        final dashboardService = DashboardService();
        final deepData = await dashboardService.getScanDetails(scanId.toString());

        Navigator.pop(context);

        if (deepData != null) {
          final deepScan = ScanModel.fromJson(deepData);
          Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => ScanDetailsScreen(scanData: deepScan)),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('عذرا، تعذر فتح تفاصيل هذا الفحص.', textAlign: TextAlign.right)),
          );
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthController()),
        ChangeNotifierProvider(create: (_) => DashboardController()),
      ],
      child: MaterialApp(
        navigatorKey: navigatorKey,
        title: 'SecureMail',
        debugShowCheckedModeBanner: false,
        
        supportedLocales: const [
          Locale('ar', 'AE'), 
        ],
        localizationsDelegates: const [
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        locale: const Locale('ar', 'AE'),

        theme: ThemeData.dark().copyWith(
          scaffoldBackgroundColor: const Color(0xFF0F172A),
          colorScheme: const ColorScheme.dark(
            primary: Colors.cyanAccent,
            secondary: Colors.blueAccent,
          ),
        ),
        home: const SplashScreen(),
      ),
    );
  }
}
