import type { AppMessageCatalog } from "../en-US/app";

export const appMessagesTrTr = {
  brand: "Appointment CRM",
  navigation: {
    status: "Sistem durumu",
    login: "Giriş yap",
    account: "Hesabım",
  },
  shell: {
    skipToContent: "Ana içeriğe geç",
    portfolioNotice: "Güvenli randevu operasyonları",
  },
  status: {
    eyebrow: "Platform temeli",
    title: "Sistem hazırlık durumu",
    description:
      "Bu ekran Lily UI frontend'in ASP.NET Core API'ye ve PostgreSQL readiness kontrolüne ulaşabildiğini kanıtlar.",
    api: "API",
    database: "PostgreSQL",
    loading: "Servisler kontrol ediliyor…",
    healthy: "Sağlıklı",
    unavailable: "Erişilemiyor",
    retry: "Yeniden kontrol et",
    traceId: "Trace ID",
    error: "Readiness yanıtı alınamadı.",
  },
  login: {
    eyebrow: "Kimlik doğrulama",
    title: "İşletme alanınıza giriş yapın",
    description:
      "Hesap bilgilerinizi kullanın. Birden fazla işletmeye üyeyseniz doğrulama sonrasında aktif işletmeyi seçebilirsiniz.",
    email: "E-posta adresi",
    password: "Parola",
    tenant: "İşletme",
    submit: "Devam et",
    submitting: "Giriş yapılıyor…",
    validation: "Geçerli bir e-posta adresi ve parola girin.",
    tenantRequired: "Devam etmek için bir işletme seçin.",
    error: "Hesap bilgileri geçersiz veya hesap kullanılamıyor.",
    securityNotice:
      "Access token yalnızca uygulama belleğinde tutulur. Döndürülen refresh credential HttpOnly cookie içindedir ve browser storage alanlarına açılmaz.",
  },
  auth: {
    initializing: "Güvenli oturum geri yükleniyor…",
  },
  account: {
    eyebrow: "Güvenli oturum",
    role: "Rol",
    tenant: "Aktif işletme",
    switchTenant: "İşletmeyi değiştir",
    logout: "Çıkış yap",
    revokeAll: "Tüm oturumları kapat",
    tenantLoadError: "Kullanılabilir işletmeler yüklenemedi.",
    switchError: "İşletme değiştirilemedi.",
    logoutError: "Oturum güvenli biçimde kapatılamadı. Lütfen yeniden deneyin.",
  },
  error: {
    title: "Uygulama görüntülenemedi",
    description: "Sayfayı yenileyin. Sorun sürerse API trace bilgisini kullanın.",
    reload: "Yenile",
  },
} as const satisfies AppMessageCatalog;
