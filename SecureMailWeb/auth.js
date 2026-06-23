// دالة عرض الإشعارات المنبثقة (Toasts) بدلاً من alerts المزعجة
function showNotice(text, type) {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        document.body.appendChild(container);
    }
    
    // Map existing types ok/bad to success/error for styling
    const toastType = type === 'ok' ? 'success' : 'error';
    
    const toast = document.createElement('div');
    toast.className = `toast ${toastType}`;
    
    const timeId = new Date().getTime();
    toast.id = `toast-${timeId}`;
    
    toast.innerHTML = `
        <div>${text}</div>
        <div class="toast-close" onclick="this.parentElement.remove()">✕</div>
    `;
    
    container.appendChild(toast);

    // إخفاء تلقائي بعد 5 ثواني
    setTimeout(() => {
        toast.style.animation = 'slideInRight 0.4s reverse forwards';
        setTimeout(() => toast.remove(), 400);
    }, 5000);
}

// --- أولاً: منطق تسجيل الدخول (Login) ---
const loginForm = document.getElementById('loginForm');
if (loginForm) {
    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitBtn = document.getElementById('submitBtn');

        const loginData = {
            email: document.getElementById('email').value,
            password: document.getElementById('password').value
        };

        try {
            submitBtn.disabled = true;
            submitBtn.innerText = "جاري التحقق...";

            const response = await fetch('http://localhost:5275/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(loginData)
            });

            const result = await response.json();

            if (response.ok) {
                // حفظ التوكن في ذاكرة المتصفح (LocalStorage) لدوام الجلسة
                localStorage.setItem('secureMailToken', result.token);

                showNotice(result.message, "ok");

                // التوجه للداشبورد بعد النجاح
                setTimeout(() => { window.location.href = 'dashboard.html'; }, 1500);
            } else {
                // رسالة الخطأ (مثل: بيانات غير صحيحة أو حساب معطل)
                showNotice(result.message || "خطأ في تسجيل الدخول", "bad");
            }
        } catch (error) {
            showNotice("فشل الاتصال بالسيرفر! تأكد من تشغيل الـ API.", "bad");
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerText = "دخول / Login";
        }
    });
}

// --- ثانياً: منطق إنشاء الحساب (Register) ---
const registerForm = document.getElementById('registerForm');
if (registerForm) {
    registerForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitBtn = document.getElementById('submitBtn');

        const formData = {
            fullName: document.getElementById('fullName').value,
            email: document.getElementById('email').value,
            phone: document.getElementById('phone').value,
            password: document.getElementById('password').value,
            confirmPassword: document.getElementById('confirmPassword').value
        };

        if (formData.password !== formData.confirmPassword) {
            showNotice("كلمتا المرور غير متطابقتين!", "bad");
            return;
        }

        try {
            submitBtn.disabled = true;
            submitBtn.innerText = "جاري المعالجة...";

            const response = await fetch('http://localhost:5275/api/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (response.ok) {
                showNotice(result.message, "ok");
                // التعديل هنا: تحويل المستخدم لصفحة التحقق مع تمرير إيميله في الرابط
                setTimeout(() => { window.location.href = `verify.html?email=${encodeURIComponent(formData.email)}`; }, 2000);
            } else {
                showNotice(result.message || "حدث خطأ في التسجيل", "bad");
            }
        } catch (error) {
            showNotice("فشل الاتصال بالسيرفر!", "bad");
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerText = "إنشاء الحساب الآن";
        }
    });
}

// --- ثالثاً: منطق تفعيل الحساب (Verify OTP) ---
const verifyForm = document.getElementById('verifyForm');
if (verifyForm) {
    verifyForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitBtn = document.getElementById('submitBtn');

        const verifyData = {
            email: document.getElementById('verifyEmail').value,
            otpCode: document.getElementById('otpCode').value
        };

        try {
            submitBtn.disabled = true;
            submitBtn.innerText = "جاري التحقق...";

            const response = await fetch('http://localhost:5275/api/auth/verify', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(verifyData)
            });

            const result = await response.json();

            if (response.ok) {
                showNotice(result.message, "ok");

                // التوجه لصفحة تسجيل الدخول بعد التفعيل بنجاح
                setTimeout(() => { window.location.href = 'login.html'; }, 2000);
            } else {
                showNotice(result.message || "رمز غير صحيح", "bad");
            }
        } catch (error) {
            showNotice("فشل الاتصال بالسيرفر! تأكد من تشغيل الـ API.", "bad");
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerText = "تفعيل الحساب";
        }
    });
}