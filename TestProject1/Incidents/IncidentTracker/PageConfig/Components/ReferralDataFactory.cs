using Microsoft.Playwright;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Text;
using static CareAdminTestProject.Common.PlaywrightExtensions;
using Log = CareAdminTestProject.Common.TestLog;


namespace CareAdminTestProject.Incidents.IncidentTracker.PageConfig.Components
{
    public class ReferralDataFactory: IncidentTrackerPage
    {
        public ReferralDataFactory(IPage page) : base(page)
        {
        }

        /// <summary>
        /// Сквозная цепочка API-запросов: берет типы инцидентов, резидента, создает Жалобу и генерирует Реферал.
        /// </summary>
        /// <returns>Возвращает имя созданного резидента для верификации его в UI-гриде.</returns>
        public async Task<string> PrepareReferralDataAsync()
        {
            Log.Information("[API FACTORY] Начинаем подготовку данных для теста рефералов...");

            // 1. ПОЛУЧАЕМ ТИП ИНЦИДЕНТА (incidentTypeId)
            string incidentTypesJson = await _page.ApiGetRequest("/api/incident-types");
            using var typesDoc = JsonDocument.Parse(incidentTypesJson);
            // Берем ID первого попавшегося типа (например, "Attempted Suicide" или "Burns")
            string incidentTypeId = typesDoc.RootElement.GetProperty("incidentTypes")[0].GetProperty("id").GetString()
                ?? throw new Exception("Не удалось распарсить incidentTypeId");

            Log.Debug($"[API FACTORY] Шаг 1: Выбран incidentTypeId = '{incidentTypeId}'");

            // 2. ПОЛУЧАЕМ РЕЗИДЕНТА И ЕГО ЛОКАЦИЮ (residentId, unitId)
            // Запрашиваем активных резидентов по текущему facility (используем твой эндпоинт списков)
            string residentsJson = await _page.ApiGetRequest("/api/Residents/active"); // подставь точный урл, если он другой
            using var residentsDoc = JsonDocument.Parse(residentsJson);

            // Берем первого или второго резидента из массива
            var targetResident = residentsDoc.RootElement[1];
            string residentId = targetResident.GetProperty("id").GetString() ?? "";
            string residentName = targetResident.GetProperty("fullName").GetString() ?? "";
            string unitId = targetResident.GetProperty("unitId").GetString() ?? "";

            Log.Debug($"[API FACTORY] Шаг 2: Выбран резидент '{residentName}' (ID: {residentId}, Unit: {unitId})");

            // 3. СОЗДАЕМ ЖАЛОБУ (Grievance)
            // Собираем точный пейлоад на основе твоего скриншота Network
            var grievancePayload = new
            {
                grievance = new
                {
                    residentId = residentId,
                    unitId = unitId,
                    attachments = new string[] { },
                    categoryId = "f0e764fa-35bf-44be-b814-b0cfe7f705d1", // Используем категорию со скриншота
                    complainantsSatisfied = false,
                    departmentId = "12da63db-a057-42b4-99b4-b00a57c0720b",
                    dueDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-ddT00:00:00"),
                    facilityId = "c1f80483-fd30-4327-814e-778ad171a67b", // Твой X-Context-Id
                    meansOfComplaint = 1,
                    noEvidenceOfAbuse = false,
                    received = DateTime.UtcNow.ToString("yyyy-MM-ddT00:00:00"),
                    referredToDohDph = false,
                    resolved = false,
                    roomNumber = "619",
                    signatures = new string[] { },
                    sourceOfComplaint = 0
                }
            };

            Log.Information($"[API FACTORY] Отправляем POST на создание Жалобы для резидента {residentName}...");
            string grievanceResponseJson = await _page.ApiPostRequest("/api/Grievances", grievancePayload);

            using var grievanceDoc = JsonDocument.Parse(grievanceResponseJson);
            string grievanceId = grievanceDoc.RootElement.GetProperty("id").GetString()
                ?? throw new Exception("Бэкенд не вернул id созданной жалобы");

            Log.Debug($"[API FACTORY] Шаг 3: Жалоба успешно создана. grievanceId = '{grievanceId}'");

            // 4. ГЕНЕРИРУЕМ РЕФЕРАЛ (Referral)
            // Пейлоад из твоего второго сообщения
            var referralPayload = new
            {
                grievanceId = grievanceId,
                date = DateTime.UtcNow.ToString("yyyy-MM-ddT00:00:00"),
                incidentTypeId = incidentTypeId
            };

            Log.Information($"[API FACTORY] Отправляем POST на генерацию реферала...");
            await _page.ApiPostRequest("/api/Grievances/referral", referralPayload);

            Log.Information($"[API FACTORY SUCCESS] Реферал для {residentName} готов к UI-тестам!");

            return residentName; // Возвращаем имя резидента, чтобы тест знал, кого искать в гриде
        }
    }
}
