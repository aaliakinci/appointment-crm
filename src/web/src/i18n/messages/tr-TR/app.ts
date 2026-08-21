import type { AppMessageCatalog } from "../en-US/app";

export const appMessagesTrTr = {
  brand: "Appointment CRM",
  navigation: {
    status: "Sistem durumu",
    login: "Giriş yap",
  },
  shell: {
    skipToContent: "Ana içeriğe geç",
    portfolioNotice: "Faz 1 teknik iskeleti",
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
    title: "Giriş akışı Faz 2'de geliyor",
    description:
      "Kimlik doğrulama öncesi uygulama kabuğu ve route sınırı hazır. Credentials ve güvenli session davranışı Faz 1'de taklit edilmiyor.",
    back: "Sistem durumunu görüntüle",
  },
  error: {
    title: "Uygulama görüntülenemedi",
    description: "Sayfayı yenileyin. Sorun sürerse API trace bilgisini kullanın.",
    reload: "Yenile",
  },
} as const satisfies AppMessageCatalog;
