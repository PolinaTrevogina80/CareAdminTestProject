using System;
using System.Collections.Generic;
using System.Text;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    internal class IncidentMedicationTests : BaseIncidentTests
    {
        [Test]
        public async Task MedicationTab_GlobalValidation_ShouldToggleTabRedDotBasedOnInput()
        {
            var tab = "Medication";
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);

            // Изначально вкладка пустая — красная точка должна быть
            await steps.VerifyRedDotTab(tab, true);

            // Заполняем только один символ в первой строке (индекс 0)
            await steps.FillMedicationRowAsync(rowIndex: 0, medName: "A");

            // Точка должна мгновенно исчезнуть
            await steps.VerifyRedDotTab(tab, false);

            // Стираем текст из этой строки
            await steps.ClearMedicationRowAsync(rowIndex: 0);

            // Точка должна вернуться
            await steps.VerifyRedDotTab(tab, true);
        }

        [Test]
        public async Task MedicationTab_DynamicRowAddition_ShouldAddIndependentRows()
        {
            var tab = "Medication";
            await steps.FillGeneralAndDetailsTabsAsync(data);
            await steps.SwitchToTab(tab);

            // По умолчанию на экране может быть 1 строка. Нажимаем кнопку "Add" еще 2 раза
            await steps.ClickAddMedicationAsync(clicksCount: 2);

            // Проверяем, что на экране физически отображается ровно 3 строки полей
            await steps.VerifyMedicationRowsCountAsync(expectedCount: 3);

            // Заполняем первую и вторую строки разными данными
            await steps.FillMedicationRowAsync(rowIndex: 0, medName: "Med-1", dosage: "10mg");
            await steps.FillMedicationRowAsync(rowIndex: 1, medName: "Med-2", dosage: "20mg");

            // Проверяем независимость: данные в первой строке не перезаписались
            await steps.VerifyMedicationRowDataAsync(rowIndex: 0, expectedMedName: "Med-1");
            await steps.VerifyMedicationRowDataAsync(rowIndex: 1, expectedMedName: "Med-2");
        }

        [Test]
        public async Task MedicationTab_DeleteRecords_ShouldPreserveCorrectOrderAndTriggerValidation()
        {
            var tab = "Medication";
            await steps.FillGeneralAndDetailsTabsAsync(data);
            await steps.SwitchToTab(tab);

            await steps.ClickAddMedicationAsync(clicksCount: 2); // Получаем 3 строки

            // Заполняем их как "1", "2", "3"
            await steps.FillMedicationRowAsync(rowIndex: 0, medName: "1");
            await steps.FillMedicationRowAsync(rowIndex: 1, medName: "2");
            await steps.FillMedicationRowAsync(rowIndex: 2, medName: "3");

            // Удаляем ВТОРУЮ строку (индекс 1)
            await steps.ClickDeleteMedicationRowAsync(rowIndex: 1);

            // На экране должно остаться 2 строки
            await steps.VerifyMedicationRowsCountAsync(expectedCount: 2);

            // Бывшая третья строка ("3") теперь должна сместиться на индекс 1
            await steps.VerifyMedicationRowDataAsync(rowIndex: 0, expectedMedName: "1");
            await steps.VerifyMedicationRowDataAsync(rowIndex: 1, expectedMedName: "3");

            // Удаляем оставшиеся строки, возвращая вкладку в пустое состояние
            await steps.ClickDeleteMedicationRowAsync(rowIndex: 1);
            await steps.ClickDeleteMedicationRowAsync(rowIndex: 0);

            // Красная точка валидации вкладки должна вернуться
            await steps.VerifyRedDotTab(tab, expected: true);
        }

        [Test]
        public async Task MedicationTab_EmptyRowEdgeCases_ShouldIgnoreEmptyRowsOnSave()
        {
            var tab = "Medication";
            await steps.FillGeneralAndDetailsTabsAsync(data);
            await steps.SwitchToTab(tab);

            // Заполняем первую строку
            await steps.FillMedicationRowAsync(rowIndex: 0, medName: "Aspirin");

            // Добавляем вторую строку, но оставляем её абсолютно пустой
            await steps.ClickAddMedicationAsync(clicksCount: 1);

            // Сохраняем форму
            await steps.ClickSaveMedicationAsync();

            // Перезагружаем вкладку/форму или проверяем текущее состояние: 
            // пустая строка должна либо удалиться автоматически, либо не вызывать ошибку валидации
            await steps.VerifyMedicationRowsCountAsync(expectedCount: 1);
            await steps.VerifyRedDotTab(tab, expected: false);
        }


    }
}
