/**
 * Etüt Arkadaşım - Genel JavaScript Fonksiyonları
 */

// 1. PROFIL RESMI ÖNİZLEME (Profil Düzenleme Sayfası İçin)
// Kullanıcı dosya seçtiği an, sayfayı yenilemeden yeni resmi gösterir.
function previewProfileImage(input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            // Profil sayfasındaki img etiketinin ID'si 'profilePreview' olmalı
            const preview = document.getElementById('profilePreview');
            if (preview) {
                preview.src = e.target.result;
            }
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// 2. EŞLEŞME SONLANDIRMA (Dashboard Sayfası İçin)
// Backend: StudyRequestController > EndMatch
async function endMatch(friendId) {
    if (!confirm("Bu kullanıcıyla olan eşleşmenizi sonlandırmak istediğinize emin misiniz? Bu işlem geri alınamaz.")) {
        return;
    }

    try {
        const response = await fetch(`/api/request/endmatch/${friendId}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (response.ok) {
            alert(result.message);
            // Sayfadaki arkadaş listesini yenilemek için sayfayı reload yapabiliriz
            // veya loadMyFriends() fonksiyonun dashboard'da varsa onu çağırabiliriz.
            location.reload();
        } else {
            alert("Hata: " + result.message);
        }
    } catch (error) {
        console.error("Eşleşme sonlandırılamadı:", error);
        alert("İşlem sırasında bir hata oluştu.");
    }
}

// 3. DERS SİLME (Dashboard Sayfası İçin)
// Backend: CoursesController > RemoveCourse
async function removeCourse(courseId) {
    if (!confirm("Bu dersi seçtiğiniz dersler listenizden çıkarmak istiyor musunuz?")) {
        return;
    }

    try {
        const response = await fetch(`/api/courses/remove/${courseId}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (response.ok) {
            // Dashboard'daki ders listesini yenile
            if (typeof loadMyCourses === 'function') {
                loadMyCourses();
            } else {
                location.reload();
            }
        } else {
            alert("Hata: " + result.message);
        }
    } catch (error) {
        console.error("Ders silinemedi:", error);
    }
}

// 4. BOOTSTRAP TOOLTIP AKTİFLEŞTİRME
// Sitedeki ipucu balonlarını (hover mesajları) çalıştırır.
document.addEventListener("DOMContentLoaded", function () {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });
});