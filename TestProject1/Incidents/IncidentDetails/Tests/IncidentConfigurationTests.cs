using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    [TestFixture]
    internal class IncidentConfigurationTests : BaseIncidentTests
    {
        [Test]
        [Description("Verify that adding an individual employee restricts or expands UI dropdown, and removing them reverts it correctly.")]
        // === НАСТРОЙКА КОНФИГУРАЦИЙ ТЕСТА ===
        [TestCase("supervisorConfiguration", "Supervisor")]
        [TestCase("cnaConfiguration", "CNA")]
        [TestCase("chargeNurseConfiguration", "Charge nurse")]
        public async Task StaffListsAddAndRemoveIndividualEmployee(string sectionRole, string uiRoleName)
        {
            {

                // 1. Узнаем реальное состояние конфигурации из API до каких-либо манипуляций
                string currentConfigJson = await steps.GetEmployeeConfigurationAsync();
                var configNode = System.Text.Json.Nodes.JsonNode.Parse(currentConfigJson);
                var roleConfig = configNode?["incidentConfiguration"]?[sectionRole];

                string arrayName = roleConfig?["userConstraints"] != null ? "userConstraints" : "employeeConstraints";
                var constraintsArray = roleConfig?[arrayName]?.AsArray();

                // Флаг: была ли конфигурация изначально абсолютно пустой
                bool isInitiallyEmpty = constraintsArray == null || constraintsArray.Count == 0;


                // 1. Предусловия: Считаем базовое количество в UI через обновленный метод
                await steps.UnlockStaffSectionByFillingDateAsync();
                int initialUiCount = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);

                // 2. Поиск кандидата на добавление (запоминаем Id и Имя)
                var (employeeId, employeeName) = await steps.GetAvailableActiveEmployeeAsync(sectionRole);
                Log.Information($"[TEST] Выбран сотрудник для теста: {employeeName} ({employeeId})");

                // ==================== ДЕЙСТВИЕ 1: ДОБАВЛЕНИЕ ====================
                await steps.ModifyEmployeeConstraintAsync(sectionRole, employeeId, isAdding: true);

                // Перегружаем форму и разблокируем её датой
                await steps.ReloadNewIncidentPage();
                await steps.UnlockStaffSectionByFillingDateAsync();

                // Проверяем количество после добавления
                int uiCountAfterAdd = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);

                if (isInitiallyEmpty)
                {
                    // ИСПРАВЛЕНИЕ: Если было пусто, UI сбросил 424 человека и оставил ровно 1 (нашего добавленного)
                    Assert.That(uiCountAfterAdd, Is.EqualTo(1),
                        $"Конфигурация была пустой (в UI было {initialUiCount}), после добавления фильтр должен сузить список до 1 сотрудника.");
                }
                else
                {
                    // Если в конфиге уже кто-то был (например, 2 человека), то список расширился до 3
                    Assert.That(uiCountAfterAdd, Is.EqualTo(initialUiCount + 1),
                        $"Количество опций в UI не увеличилось на 1 от базового. Ожидалось: {initialUiCount + 1}, стало: {uiCountAfterAdd}");
                }

                // В обоих случаях наш сотрудник обязан быть в списке
                await steps.VerifyEmployeeInDropdownAsync(uiRoleName, employeeName, shouldBePresent: true);


                // ==================== ДЕЙСТВИЕ 2: УДАЛЕНИЕ ====================
                await steps.ModifyEmployeeConstraintAsync(sectionRole, employeeId, isAdding: false);

                // Снова перегружаем форму
                await steps.ReloadNewIncidentPage();
                await steps.UnlockStaffSectionByFillingDateAsync();

                // Проверяем возврат к исходному состоянию
                int uiCountAfterCleanup = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);

                if (isInitiallyEmpty)
                {
                    // ИСПРАВЛЕНИЕ: Мы удалили единственного человека, фильтр снова стал пустым, шлюзы открылись, и вернулись все 424+ сотрудника
                    Assert.That(uiCountAfterCleanup, Is.EqualTo(initialUiCount),
                        $"После удаления единственного сотрудника список должен был вернуться к дефолтному состоянию 'все доступны' ({initialUiCount}), но в UI: {uiCountAfterCleanup}");
                }
                else
                {
                    // Если кто-то был изначально, просто возвращаемся к исходному набору фильтров
                    Assert.That(uiCountAfterCleanup, Is.EqualTo(initialUiCount),
                        $"Количество опций в UI не вернулось к исходному. Ожидалось: {initialUiCount}, стало: {uiCountAfterCleanup}");
                }
            }
        }

        [Test]
        [Description("Verify that if configuration becomes completely empty, the UI dropdown expands to show all employees (400+).")]
        [TestCase("supervisorConfiguration", "Supervisor")]
        [TestCase("cnaConfiguration", "CNA")]
        [TestCase("chargeNurseConfiguration", "Charge Nurse")]
        public async Task StaffListsClearAllConstraintsOpensFullDropdown(string sectionRole, string uiRoleName)
        {
            // 1. Запрашиваем текущую конфигурацию из API
            string currentConfigJson = await steps.GetEmployeeConfigurationAsync();
            var configNode = System.Text.Json.Nodes.JsonNode.Parse(currentConfigJson);
            var roleConfig = configNode?["incidentConfiguration"]?[sectionRole];

            string arrayName = roleConfig?["userConstraints"] != null ? "userConstraints" : "employeeConstraints";
            var constraintsArray = roleConfig?[arrayName]?.AsArray();

            int existingConstraintsCount = constraintsArray?.Count ?? 0;
            Log.Information($"[TEST] [{uiRoleName}] Текущее количество сотрудников в конфиге API: {existingConstraintsCount}");

            // ==================== ПОДГОТОВКА СОСТОЯНИЯ ====================
            // Если в конфиге КТО-ТО ЕСТЬ, нам нужно очистить массив, чтобы проверить сброс до 400+
            if (existingConstraintsCount > 0)
            {
                Log.Information($"[TEST] [{uiRoleName}] Конфиг не пуст ({existingConstraintsCount} эл.). Очищаем массив для проверки полного сброса...");

                // Полностью очищаем массив ограничений для этой роли
                constraintsArray.Clear();

                // Отправляем пустой конфиг на сервер и дожидаемся ответа сети
                await steps.ModifyEmployeeConstraintAsync(sectionRole, "dummyId", isAdding: false);

            }
            else
            {
                Log.Information($"[TEST] [{uiRoleName}] Конфиг уже пуст. Проверяем дефолтное состояние UI (400+)...");
            }

            // ==================== ПРОВЕРКА UI ====================
            // Перегружаем форму, чтобы Angular скачал обновленный пустой конфиг
            await steps.ReloadNewIncidentPage();
            await steps.UnlockStaffSectionByFillingDateAsync();

            // Считаем количество опций в выпадающем списке
            int uiCountAfterClear = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Log.Information($"[TEST] [{uiRoleName}] Количество элементов в UI после очистки конфига: {uiCountAfterClear}");

            // Проверяем, что шлюзы открылись (ждем большую дефолтную базу, например, больше 100 или ровно 424)
            Assert.That(uiCountAfterClear, Is.GreaterThan(100),
                $"При пустой конфигурации {sectionRole} выпадающий список {uiRoleName} должен содержать всех доступных сотрудников (400+), но найдено: {uiCountAfterClear}");
        }

        [Test]
        [Description("Verify that adding a job title restricts or expands UI dropdown to its respective employees, and removing it reverts changes.")]
        [TestCase("supervisorConfiguration", "Supervisor")]
        [TestCase("cnaConfiguration", "CNA")]
        [TestCase("chargeNurseConfiguration", "Charge Nurse")]
        public async Task StaffListsAddAndRemoveJobTitleConstraint(string sectionRole, string uiRoleName)
        {


            // Считываем базовое количество сотрудников в UI
            await steps.UnlockStaffSectionByFillingDateAsync();
            int initialUiCount = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);

            // Проверяем исходную пустоту конфигурации через API
            string currentConfigJson = await steps.GetEmployeeConfigurationAsync();
            var configNode = System.Text.Json.Nodes.JsonNode.Parse(currentConfigJson);
            var roleConfig = configNode?["incidentConfiguration"]?[sectionRole];

            var employeesArray = roleConfig?["employeeConstraints"]?.AsArray();
            var jobTitlesArray = roleConfig?["jobTitleConstraints"]?.AsArray();

            bool isInitiallyEmpty = (employeesArray == null || employeesArray.Count == 0) &&
                                    (jobTitlesArray == null || jobTitlesArray.Count == 0);

            Log.Information($"[TEST] [{uiRoleName}] Исходно сотрудников в UI: {initialUiCount}. Конфиг пустой? {isInitiallyEmpty}");

            // Получаем доступную должность
            var (jobTitleId, jobTitleName) = await steps.GetAvailableJobTitleAsync(sectionRole);
            Log.Information($"[TEST] [{uiRoleName}] Выбрана должность для теста: {jobTitleName} ({jobTitleId})");

            // ==================== ДЕЙСТВИЕ 1: ДОБАВЛЕНИЕ ДОЛЖНОСТИ ====================
            await steps.ModifyJobTitleConstraintAsync(sectionRole, jobTitleId, isAdding: true);

            // Перегружаем UI и разблокируем датой
            await steps.ReloadNewIncidentPage();
            await steps.UnlockStaffSectionByFillingDateAsync();

            // Даем фронтенду до 1.5 секунд, чтобы точно переварить изменения в DOM
            await Page.WaitForTimeoutAsync(1500);

            int uiCountAfterAddJobTitle = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Log.Information($"[TEST] [{uiRoleName}] Сотрудников в UI после добавления должности: {uiCountAfterAddJobTitle}");

            if (isInitiallyEmpty)
            {
                // Мягкая проверка: Было 424. Фильтр по должности обязан был уменьшить (сузить) этот список
                Assert.That(uiCountAfterAddJobTitle, Is.LessThan(initialUiCount),
                    $"Список должен был сузиться до сотрудников с должностью '{jobTitleName}', но остался полным ({uiCountAfterAddJobTitle}).");
            }
            else
            {
                // Если кто-то уже был, то добавление целой должности обязано расширить список
                Assert.That(uiCountAfterAddJobTitle, Is.GreaterThan(initialUiCount),
                    $"Количество сотрудников в UI не увеличилось после добавления должности.");
            }

            // ==================== ДЕЙСТВИЕ 2: УДАЛЕНИЕ ДОЛЖНОСТИ ====================
            await steps.ModifyJobTitleConstraintAsync(sectionRole, jobTitleId, isAdding: false);

            // Снова перегружаем UI
            await steps.ReloadNewIncidentPage();
            await steps.UnlockStaffSectionByFillingDateAsync();

            // Снова даем паузу для стабильности рендера
            await Page.WaitForTimeoutAsync(1500);

            int uiCountAfterCleanup = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Log.Information($"[TEST] [{uiRoleName}] Сотрудников в UI после удаления должности: {uiCountAfterCleanup}");

            // Математически точный возврат к исходному состоянию (хоть для 424, хоть для любого другого числа)
            Assert.That(uiCountAfterCleanup, Is.EqualTo(initialUiCount),
                $"После удаления должности список в UI не вернулся к исходному состоянию.");
        }


            [Test]
            public async Task AdminListsAddAndRemoveUserConstraint()
            {
                string targetUserId = "Test, Polly";

                // Строковые роли для основных подписей (как в твоем свитч-кейсе SignAsRoleAsync)
                const string DNS_ROLE = "Director of Nursing";
                const string MD_ROLE = "Medical Director";
                const string ADMIN_ROLE = "Administrator";

                // ========================================== 
                // ШАГ 1: Полная очистка прав перед тестом 
                // ========================================== 
                await steps.ModifyUserRoleConstraintByNameAsync("directorOfNursingConfiguration", targetUserId, isAdding: false);
                await steps.ModifyUserRoleConstraintByNameAsync("medicalDirectorConfiguration", targetUserId, isAdding: false);
                await steps.ModifyUserRoleConstraintByNameAsync("administratorConfiguration", targetUserId, isAdding: false);

                // Заполнение и создание инцидента 
                await steps.ClearGeneralForm();
                await steps.FillGeneralTabAsync(data);
                await steps.FillDetailsTabAsync(data);
                await steps.FillStateTabAsync(data);
                await steps.FillMedicationTabAsync(data);
                await steps.FillRNFormTabAsync(data);
                await steps.ClickCreateIncidentAsync();
                await steps.FillSummaryTabAsync(data);
                await steps.ClickSaveIncidentAsync();
                await steps.SwitchToTab("Summary");

                // ========================================== 
                // ШАГ 2: Проверка отсутствия кнопки подписи Саммари
                // ========================================== 
                // Пользователя нет в конфиге — изолированная кнопка Саммари должна быть скрыта
                await steps.AssertSummarySignatureButtonIsHiddenAsync();

                // Возвращаем права Директора, чтобы продолжить тест 
                await steps.ModifyUserRoleConstraintByNameAsync("directorOfNursingConfiguration", targetUserId, isAdding: true);

                // Перезагружаем страницу, чтобы UI обновился, и возвращаемся на вкладку
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");

                // Теперь кнопка Саммари ОБЯЗАНА появиться (проверяем видимость)
                await steps.AssertSummarySignatureButtonIsVisibleAsync();

                // Подписываем Саммари (первая подпись) 
                await steps.SignSummaryAndVerifyAsync();
                await steps.ClickSaveIncidentAsync(true);
                await steps.UploadAttachmentTabAsync("Accident Report");

                // ========================================== 
                // ШАГ 2.5: Проверка ОСНОВНОЙ подписи DNS (после Саммари)
                // ========================================== 
                // Саммари подписано. Убираем роль DNS и проверяем, что ОСНОВНАЯ подпись DNS скрыта
                await steps.ModifyUserRoleConstraintByNameAsync("directorOfNursingConfiguration", targetUserId, isAdding: false);
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");
                await steps.AssertPrimarySignatureButtonIsHiddenAsync(DNS_ROLE);

                // Возвращаем роль DNS, проверяем, что кнопка появилась (Visible) и подписываем
                await steps.ModifyUserRoleConstraintByNameAsync("directorOfNursingConfiguration", targetUserId, isAdding: true);
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");
                await steps.AssertPrimarySignatureButtonIsVisibleAsync(DNS_ROLE);

                // Твой стандартный метод подписания за DNS
                await steps.SignDNS();

                // ========================================== 
                // ШАГ 3: Проверка зависимых подписей (MD и Admin) 
                // ========================================== 
                // Первая подпись (DNS) поставлена. Блоки MD и Admin появились на экране, 
                // но кнопок "Sign Here" внутри них быть не должно, так как мы удалили права на Шаге 1 
                await steps.AssertPrimarySignatureButtonIsHiddenAsync(MD_ROLE);
                await steps.AssertPrimarySignatureButtonIsHiddenAsync(ADMIN_ROLE);

                // ========================================== 
                // ШАГ 4: Проверка и установка подписи Medical Director 
                // ========================================== 
                await steps.ModifyUserRoleConstraintByNameAsync("medicalDirectorConfiguration", targetUserId, isAdding: true);
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");

                // ИСПРАВЛЕНО: Кнопка у MD появилась (IsVisible), а у Admin — всё еще строго скрыта (IsHidden)
                await steps.AssertPrimarySignatureButtonIsVisibleAsync(MD_ROLE);
                await steps.AssertPrimarySignatureButtonIsHiddenAsync(ADMIN_ROLE);

                // Подписываем за MD (вызываем твой существующий метод) 
                await steps.SignMD();

                // ========================================== 
                // ШАГ 5: Проверка и установка подписи Administrator 
                // ========================================== 
                await steps.ModifyUserRoleConstraintByNameAsync("administratorConfiguration", targetUserId, isAdding: true);
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");

                // ИСПРАВЛЕНО: Теперь кнопка у Администратора доступна (IsVisible)
                await steps.AssertPrimarySignatureButtonIsVisibleAsync(ADMIN_ROLE);

                // Подписываем за Администратора 
                await steps.SignAdministrator();

                // Финальная проверка полной блокировки инцидента 
                await steps.AssertIncidentIsLockedAsync();
            }

        [Test]
        public async Task AdminListsAddAndRemoveRoleConstraint()
        {
            string targetUserId = "Test, Polly";
            const string DNS_ROLE = "Director of Nursing";
            const string MD_ROLE = "Medical Director";
            const string ADMIN_ROLE = "Administrator";

            // Массив секций для быстрой очистки в цикле
            string[] sections = { "directorOfNursingConfiguration", "medicalDirectorConfiguration", "administratorConfiguration" };

            try
            {
                // =========================================================================
                // ШАГ 1: ТОТАЛЬНАЯ ОЧИСТКА. Убираем и юзера индивидуально, и его шаблон роли
                // =========================================================================
                foreach (var section in sections)
                {
                    await steps.ModifyUserRoleConstraintByNameAsync(section, targetUserId, isAdding: false);
                    await steps.ModifyRoleTemplateConstraintByNameAsync(section, targetUserId, isAdding: false);
                }

                // Заполнение и создание инцидента 
                await steps.ClearGeneralForm();
                await steps.FillGeneralTabAsync(data);
                await steps.FillDetailsTabAsync(data);
                await steps.FillStateTabAsync(data);
                await steps.FillMedicationTabAsync(data);
                await steps.FillRNFormTabAsync(data);
                await steps.ClickCreateIncidentAsync();
                await steps.FillSummaryTabAsync(data);
                await steps.ClickSaveIncidentAsync();
                await steps.SwitchToTab("Summary");

                // ========================================== 
                // ШАГ 2: Проверка отсутствия кнопки подписи Саммари
                // ========================================== 
                await steps.AssertSummarySignatureButtonIsHiddenAsync();

                // Возвращаем ШАБЛОН РОЛИ для Директора (индивидуальный юзер остается удален!)
                await steps.ModifyRoleTemplateConstraintByNameAsync("directorOfNursingConfiguration", targetUserId, isAdding: true);

                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");
                await steps.AssertSummarySignatureButtonIsVisibleAsync();

                // Подписываем Саммари 
                await steps.SignSummaryAndVerifyAsync();
                await steps.ClickSaveIncidentAsync(true);
                await steps.UploadAttachmentTabAsync("Accident Report");

                // ========================================== 
                // ШАГ 2.5: Проверка ОСНОВНОЙ подписи DNS по ролям
                // ========================================== 
                await steps.ModifyRoleTemplateConstraintByNameAsync("directorOfNursingConfiguration", targetUserId, isAdding: false);
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");
                await steps.AssertPrimarySignatureButtonIsHiddenAsync(DNS_ROLE);

                await steps.ModifyRoleTemplateConstraintByNameAsync("directorOfNursingConfiguration", targetUserId, isAdding: true);
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");
                await steps.AssertPrimarySignatureButtonIsVisibleAsync(DNS_ROLE);
                await steps.SignDNS();

                // ========================================== 
                // ШАГ 3: Проверка зависимых подписей (MD и Admin) 
                // ========================================== 
                await steps.AssertPrimarySignatureButtonIsHiddenAsync(MD_ROLE);
                await steps.AssertPrimarySignatureButtonIsHiddenAsync(ADMIN_ROLE);

                // ========================================== 
                // ШАГ 4: Проверка и установка роли Medical Director 
                // ========================================== 
                await steps.ModifyRoleTemplateConstraintByNameAsync("medicalDirectorConfiguration", targetUserId, isAdding: true);
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");

                await steps.AssertPrimarySignatureButtonIsVisibleAsync(MD_ROLE);
                await steps.AssertPrimarySignatureButtonIsHiddenAsync(ADMIN_ROLE);
                await steps.SignMD();

                // ========================================== 
                // ШАГ 5: Проверка и установка роли Administrator 
                // ========================================== 
                await steps.ModifyRoleTemplateConstraintByNameAsync("administratorConfiguration", targetUserId, isAdding: true);
                await Page.ReloadAsync();
                await steps.SwitchToTab("Summary");

                await steps.AssertPrimarySignatureButtonIsVisibleAsync(ADMIN_ROLE);
                await steps.SignAdministrator();

                await steps.AssertIncidentIsLockedAsync();
            }
            finally
            {
                // =========================================================================
                // ТЕАРДАУН: Что бы ни случилось в тесте (упал или прошел), возвращаем 
                // индивидуальные права пользователя назад, чтобы не ломать соседние тесты!
                // =========================================================================
                Log.Information("[TEARDOWN] Восстановление исходной конфигурации пользователя...");
                foreach (var section in sections)
                {
                    await steps.ModifyUserRoleConstraintByNameAsync(section, targetUserId, isAdding: true);
                    await steps.ModifyRoleTemplateConstraintByNameAsync(section, targetUserId, isAdding: true);
                }
            }
        }


    }

}

