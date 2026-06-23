import 'dart:math';
import 'package:flutter/material.dart';
import 'package:animated_background/animated_background.dart';
import '../services/auth_service.dart';
import 'login_screen.dart';
import 'dashboard_screen.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> with TickerProviderStateMixin {
  late AnimationController _logoController;
  late Animation<double> _logoScaleAnimation;
  late Animation<double> _logoOpacityAnimation;

  @override
  void initState() {
    super.initState();
    _initAnimations();
    _checkAuthAndNavigate();
  }

  void _initAnimations() {
    // Logo entrance animation
    _logoController = AnimationController(
       vsync: this, 
       duration: const Duration(milliseconds: 2000)
    );
    _logoScaleAnimation = Tween<double>(begin: 0.5, end: 1.0).animate(
      CurvedAnimation(parent: _logoController, curve: Curves.easeOutBack)
    );
    _logoOpacityAnimation = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(parent: _logoController, curve: Curves.easeIn)
    );
    
    _logoController.forward();
  }

  Future<void> _checkAuthAndNavigate() async {
    // Allow animation to play beautifully
    await Future.delayed(const Duration(seconds: 4));
    
    if (mounted) {
       Navigator.pushReplacement(
          context, 
          PageRouteBuilder(
            pageBuilder: (_, __, ___) => const LoginScreen(),
            transitionsBuilder: (_, a, __, c) => FadeTransition(opacity: a, child: c),
            transitionDuration: const Duration(milliseconds: 800)
          )
       );
    }
  }

  @override
  void dispose() {
    _logoController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF070B19), // Deep space blue/black
      body: AnimatedBackground(
        vsync: this,
        behaviour: RandomParticleBehaviour(
          options: const ParticleOptions(
            baseColor: Colors.cyanAccent,
            spawnOpacity: 0.0,
            opacityChangeRate: 0.25,
            minOpacity: 0.1,
            maxOpacity: 0.8,
            particleCount: 80,
            spawnMaxRadius: 3.0,
            spawnMaxSpeed: 15.0,
            spawnMinSpeed: 5.0,
            spawnMinRadius: 1.0,
          ),
        ),
        child: Stack(
          children: [
              // Glowing planets/nebulae
               Positioned(
                top: MediaQuery.of(context).size.height * 0.1,
                left: -50,
                child: Container(
                  width: 300,
                  height: 300,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: Colors.cyanAccent.withOpacity(0.05),
                    boxShadow: [
                      BoxShadow(color: Colors.cyanAccent.withOpacity(0.1), blurRadius: 150, spreadRadius: 50),
                    ],
                  ),
                ),
              ),
              Positioned(
                bottom: MediaQuery.of(context).size.height * 0.1,
                right: -100,
                child: Container(
                  width: 400,
                  height: 400,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: Colors.purpleAccent.withOpacity(0.05),
                    boxShadow: [
                      BoxShadow(color: Colors.purpleAccent.withOpacity(0.1), blurRadius: 150, spreadRadius: 50),
                    ],
                  ),
                ),
              ),
  
              // Logo & Title
              Center(
                child: FadeTransition(
                  opacity: _logoOpacityAnimation,
                  child: ScaleTransition(
                    scale: _logoScaleAnimation,
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Container(
                          width: 150,
                          height: 150,
                          decoration: BoxDecoration(
                             shape: BoxShape.circle,
                             color: Colors.white.withOpacity(0.05),
                             border: Border.all(color: Colors.cyanAccent.withOpacity(0.5), width: 3),
                             boxShadow: [
                                BoxShadow(color: Colors.cyanAccent.withOpacity(0.3), blurRadius: 50, spreadRadius: 10)
                             ],
                             image: const DecorationImage(
                               image: AssetImage('assets/logo.png'),
                               fit: BoxFit.cover,
                             ),
                          ),
                        ),
                        const SizedBox(height: 32),
                        const Text(
                          'SecureMail',
                          style: TextStyle(
                            fontSize: 44,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                            letterSpacing: 3,
                            shadows: [
                               Shadow(color: Colors.cyanAccent, blurRadius: 20)
                            ]
                          ),
                        ),
                        const SizedBox(height: 16),
                        const Text(
                          'حماية لبريدك',
                          style: TextStyle(
                            fontSize: 18,
                            color: Colors.white70,
                            letterSpacing: 1.2
                          ),
                        ),
                        const SizedBox(height: 48),
                        // Elegant loading indicator
                        SizedBox(
                           width: 40,
                           height: 40,
                           child: CircularProgressIndicator(
                              color: Colors.cyanAccent.withOpacity(0.8),
                              strokeWidth: 2,
                           ),
                        )
                      ],
                    ),
                  ),
                ),
              )
          ],
        ),
      ),
    );
  }
}
