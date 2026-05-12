using CareAdminTestProject.Incidents.IncidentDetails.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;
using static IncidentDataFactory;

namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    [TestFixture]
    internal class IncidentCompletenessTests : BaseIncidentTests
    {

        [Test]
        public async Task GeneralTabCompletenessVerification()
        {
            var tab = "General";

            await steps.ClearGeneralForm();

            //Проверяем, что все нужные поля с красными точками
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, true);
            //Проверяем, что Таба с красной точкой
            await steps.VerifyRedDotTab(tab, true);
            //Заполняем
            await steps.FillGeneralTabAsync(data);

            //До сохранения
            //Проверяем, что все нужные поля без красных точкек
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, false);
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
            //Проверяем, что все нужные поля без красных точкек
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, false);

        }

        [Test]
        public async Task GeneralFieldsCompletenessVerification()
        {
            var tab = "General";

            await steps.ClearGeneralForm();
            await steps.VerifyRedDotField(steps.CreatePage.General, "Date of Incident", true);
            await steps.VerifyRedDotTab(tab, true);

            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.General, data.General);
            await steps.VerifyRedDotTab(tab, false);

        }

        [Test]
        public async Task DetailsTabCompletenessVerification()
        {
            var tab = "Details";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);
            await steps.ClearDetailsForm();

            //Проверяем, что все таба с красной точкой
            await steps.VerifyRedDotTab(tab, true);


            //Проверяем, что все нужные поля с красными точками
            await steps.SwitchFirstAid(true);
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, true);
            //Заполняем
            await steps.FillDetailsTabAsync(data);

            //До сохранения
            //Проверяем, что все нужные поля без красных точкек
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, false);
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
            //Проверяем, что все нужные поля без красных точкек
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, false);

        }

        [Test]
        public async Task DetailsFieldsCompletenessVerification()
        {
            var tab = "Details";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);
            await steps.ClearDetailsForm();

            await steps.VerifyRedDotTab(tab, true);

            // Вся магия цикла теперь тут:
            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.Details, data.Details);

            await steps.VerifyRedDotTab(tab, false);

        }



        [Test]
        public async Task StateTabCompletenessVerification()
        {
            var tab = "State";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);

            //Проверяем, что все таба с красной точкой
            await steps.VerifyRedDotTab(tab, true);

            //Заполняем
            await steps.FillStateTabAsync(data);

            //До сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickSaveIncidentAsync();

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
        }

        [Test]
        public async Task StateFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("State");

            //await steps.VerifyRedDotTab("State", true);

            await steps.VerifyStateTabSpecificLogicAsync();
        }

        [Test]
        public async Task MedicationTabCompletenessVerification()
        {
            var tab = "Medication";

            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab(tab);

            //Проверяем, что все таба с красной точкой
            await steps.VerifyRedDotTab(tab, true);

            //Заполняем
            await steps.FillMedicationTabAsync(data);

            //До сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();


            await steps.ClearMedicationTabAsync();
            await steps.VerifyRedDotTab(tab, true);

            await steps.FillMedicationTabAsync(data);

            await steps.ClickSaveIncidentAsync();

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
        }

        [Test]
        public async Task MedicationFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("Medication");
            await steps.VerifyMedicationTabFullLifecycleAndIndicatorAsync();
        }

        [Test]
        public async Task RNInvestigationFormTabCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            var tab = "RN Supervisor Investigation Form";
            await steps.SwitchToTab(tab);

            //Проверяем, что все таба с красной точкой
            await steps.VerifyRedDotTab(tab, true);

            //Заполняем
            await steps.FillRNFormTabAsync(data);

            //До сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
            await steps.ClickCreateIncidentAsync();

//            await steps.ClickSaveIncidentAsync();

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
        }

        [Test]
        public async Task RnInvestigationFormFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.FillDetailsTabAsync(data);
            await steps.SwitchToTab("RN Supervisor Investigation Form");

            await steps.FillRNFormTabWithTabCheckAsync(data);

        }

        [Test]
        public async Task SummaryTabCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            var tab = "Summary";
            await steps.SwitchToTab(tab);

            //Проверяем, что все таба с красной точкой
            await steps.VerifyRedDotTab(tab, true);

            //Заполняем
            await steps.FillSummaryTabAsync(data);

            //До сохранения
            //Проверяем, что Таба c красной точкой, так как не подписана
            await steps.VerifyRedDotTab(tab, true);
            await steps.ClickSaveIncidentAsync();
            
            //После первого сохранения
            await steps.VerifyRedDotTab(tab, true);

            //Подписываем
            await steps.SignSummaryAndVerifyAsync();
            await steps.ClickSaveIncidentAsync(true);

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab(tab, false);
        }

        [Test]
        public async Task SummaryFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("Summary");

            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.Summary, data.Summary);

        }

        [Test]
        public async Task AttachmentsTabCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            // Проверяем, что таба с красной точкой перед началом загрузок
            await steps.VerifyRedDotTab(tab, true);

            int max = 10;
            int check = 5; // Граница, после которой точка должна пропасть

            for (int i = 0; i < max; i++)
            {
                // Достаем категорию по индексу цикла из статического списка класса AttachmentsTab
                string currentCategory = AttachmentsTab.AttachmentCategories[i];
                string? note = currentCategory.Equals("Other", StringComparison.OrdinalIgnoreCase)
                    ? "Test internal note for Other category"
                    : null;

                // Передаем динамически полученное имя категории в метод загрузки
                await steps.UploadAttachmentTabAsync(currentCategory, note, fileNameString: "test_1page.pdf", toScreenShot: true);
                await Page.WaitForTimeoutAsync(1000);

                // Проверяем состояние красной точки
                // Если загружено меньше 'check' файлов (индексы 0, 1, 2, 3, 4 — первые 5 файлов), точка все еще на месте
                if (i < check - 1)
                {
                    await steps.VerifyRedDotTab(tab, true);
                }
                else
                {
                    // Как только загружен 5-й файл (индекс 4 завершился, либо проверяем на итерациях с i >= 4)
                    await steps.VerifyRedDotTab(tab, false);
                }
            }
        }

        [Test]
        public async Task AttachmentsTabSingleMultyPageFileCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.ClickCreateIncidentAsync();
            var tab = "Attachments";
            await steps.SwitchToTab(tab);

            //Проверяем, что все таба с красной точкой
            await steps.VerifyRedDotTab(tab, true);

            //Заполняем
            await steps.UploadAttachmentTabAsync(AttachmentsTab.AttachmentCategories, fileNameString: "test_10pages.pdf", toScreenShot: true);
            await steps.VerifyRedDotTab(tab, false);

        }

    }
}

