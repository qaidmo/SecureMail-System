import 'package:flutter_test/flutter_test.dart';

import 'package:secure_mail_mobile/main.dart';

void main() {
  testWidgets('App loads SplashScreen smoke test', (WidgetTester tester) async {
    // Build our app and trigger a frame.
    await tester.pumpWidget(const SecureMailApp());

    // Verify that the title 'SecureMail' is present on the screen.
    expect(find.text('SecureMail'), findsOneWidget);
    
    // Verify that the splash screen subtitle text is present.
    expect(find.text('حماية الذكاء الاصطناعي لبريدك'), findsOneWidget);
  });
}
