using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    [TestFixture]
    internal class IncidentConfigurationTests : BaseIncidentTests
    {
        [Test]
        [Description("Verify that adding an individual employee expands UI dropdown and shows their name, and removing them reverts it.")]
        public async Task Test_SupervisorList_AddAndRemoveIndividualEmployee()
        {
            string sectionRole = "supervisorConfiguration";
            string uiRoleName = "Supervisor"; // Имя лейбла, как в методе SelectDropdownOptionAsync

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

            // Проверяем количество (+1) и физическое наличие имени в списке
            int uiCountAfterAdd = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Assert.That(uiCountAfterAdd, Is.EqualTo(initialUiCount + 1), "Количество опций в UI не увеличилось на 1.");
            await steps.VerifyEmployeeInDropdownAsync(uiRoleName, employeeName, shouldBePresent: true);

            // ==================== ДЕЙСТВИЕ 2: УДАЛЕНИЕ ====================
            await steps.ModifyEmployeeConstraintAsync(sectionRole, employeeId, isAdding: false);

            // Снова перегружаем форму
            await steps.ReloadNewIncidentPage();
            await steps.UnlockStaffSectionByFillingDateAsync();

            // Проверяем возврат к базовому количеству и отсутствие имени
            int uiCountAfterCleanup = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Assert.That(uiCountAfterCleanup, Is.EqualTo(initialUiCount), "Количество опций в UI не вернулось к исходному.");
            await steps.VerifyEmployeeInDropdownAsync(uiRoleName, employeeName, shouldBePresent: false);
        }

        [Test]
        [Description("Verify that adding an individual employee expands UI dropdown and shows their name, and removing them reverts it.")]
        public async Task Test_ChargeNurseList_AddAndRemoveIndividualEmployee()
        {
            string sectionRole = "chargeNurseConfiguration";
            string uiRoleName = "Charge nurse"; // Имя лейбла, как в методе SelectDropdownOptionAsync

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

            // Проверяем количество (+1) и физическое наличие имени в списке
            int uiCountAfterAdd = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Assert.That(uiCountAfterAdd, Is.EqualTo(initialUiCount + 1), "Количество опций в UI не увеличилось на 1.");
            await steps.VerifyEmployeeInDropdownAsync(uiRoleName, employeeName, shouldBePresent: true);

            // ==================== ДЕЙСТВИЕ 2: УДАЛЕНИЕ ====================
            await steps.ModifyEmployeeConstraintAsync(sectionRole, employeeId, isAdding: false);

            // Снова перегружаем форму
            await steps.ReloadNewIncidentPage();
            await steps.UnlockStaffSectionByFillingDateAsync();

            // Проверяем возврат к базовому количеству и отсутствие имени
            int uiCountAfterCleanup = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Assert.That(uiCountAfterCleanup, Is.EqualTo(initialUiCount), "Количество опций в UI не вернулось к исходному.");
            await steps.VerifyEmployeeInDropdownAsync(uiRoleName, employeeName, shouldBePresent: false);
        }

        [Test]
        [Description("Verify that adding an individual employee expands UI dropdown and shows their name, and removing them reverts it.")]
        public async Task Test_CNAList_AddAndRemoveIndividualEmployee()
        {
            string sectionRole = "cnaConfiguration";
            string uiRoleName = "CNA"; // Имя лейбла, как в методе SelectDropdownOptionAsync

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

            // Проверяем количество (+1) и физическое наличие имени в списке
            int uiCountAfterAdd = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Assert.That(uiCountAfterAdd, Is.EqualTo(initialUiCount + 1), "Количество опций в UI не увеличилось на 1.");
            await steps.VerifyEmployeeInDropdownAsync(uiRoleName, employeeName, shouldBePresent: true);

            // ==================== ДЕЙСТВИЕ 2: УДАЛЕНИЕ ====================
            await steps.ModifyEmployeeConstraintAsync(sectionRole, employeeId, isAdding: false);

            // Снова перегружаем форму
            await steps.ReloadNewIncidentPage();
            await steps.UnlockStaffSectionByFillingDateAsync();

            // Проверяем возврат к базовому количеству и отсутствие имени
            int uiCountAfterCleanup = await steps.GetMaterialDropdownOptionsCountAsync(uiRoleName);
            Assert.That(uiCountAfterCleanup, Is.EqualTo(initialUiCount), "Количество опций в UI не вернулось к исходному.");
            await steps.VerifyEmployeeInDropdownAsync(uiRoleName, employeeName, shouldBePresent: false);
        }


    }

}

