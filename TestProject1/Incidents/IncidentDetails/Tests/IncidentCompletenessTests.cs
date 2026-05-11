using CareAdminTestProject.Incidents.IncidentDetails.Steps;
using Microsoft.Extensions.Logging;
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

            await steps.ClearGeneralForm();

            //Проверяем, что все нужные поля с красными точками
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, true);
            //Проверяем, что Таба с красной точкой
            await steps.VerifyRedDotTab("General", true);
            //Заполняем
            await steps.FillGeneralTabAsync(data);

            //До сохранения
            //Проверяем, что все нужные поля без красных точкек
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, false);
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab("General", false);
            await steps.ClickCreateIncidentAsync();

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab("General", false);
            //Проверяем, что все нужные поля без красных точкек
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.General, data.General, false);

            //await steps.FillDetailsTabAsync(data);
            //await steps.FillStateTabAsync(data);
            //await steps.FillMedicationTabAsync(data);
            //await steps.FillRNFormTabAsync(data);
            //await steps.FillSummaryTabAsync(data);

            //// 6. Сохранение и подписание
            //await steps.SaveIncidentAsync();
            //await steps.SignSummaryAndVerifyAsync();
            //await steps.SaveIncidentAsync(true);

            //// 7. добавление аттачей
            //await steps.UploadAttachmentTabAsync("Other", "This is a test note");
            ////await steps.SaveIncidentAsync();

        }

        [Test]
        public async Task GeneralFieldsCompletenessVerification()
        {

            await steps.ClearGeneralForm();
            await steps.VerifyRedDotField(steps.CreatePage.General, "Date of Incident", true);
            await steps.VerifyRedDotTab("General", true);

            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.General, data.General);
            await steps.VerifyRedDotTab("General", false);

        }

        [Test]
        public async Task DetailsTabCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("Details");
            await steps.ClearDetailsForm();

            //Проверяем, что все таба с красной точкой
            await steps.VerifyRedDotTab("Details", true);


            //Проверяем, что все нужные поля с красными точками
            await steps.SwitchFirstAid(true);
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, true);
            //Заполняем
            await steps.FillDetailsTabAsync(data);

            //До сохранения
            //Проверяем, что все нужные поля без красных точкек
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, false);
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab("Details", false);
            await steps.ClickCreateIncidentAsync();

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab("Details", false);
            //Проверяем, что все нужные поля без красных точкек
            await steps.VerifyAllFieldsDotsStateAsync(steps.CreatePage.Details, data.Details, false);

        }

        [Test]
        public async Task DetailsFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.SwitchToTab("Details");
            await steps.ClearDetailsForm();

            await steps.VerifyRedDotTab("Details", true);

            // Вся магия цикла теперь тут:
            await steps.VerifyFieldsOneByOneWithFilling(steps.CreatePage.Details, data.Details);

            await steps.VerifyRedDotTab("Details", false);

        }



        [Test]
        public async Task StateTabCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.FillDetailsTabAsync(data);
            //await steps.ClickCreateIncidentAsync();
            await steps.SwitchToTab("State");

            //Проверяем, что все таба с красной точкой
            await steps.VerifyRedDotTab("State", true);

            //Заполняем
            await steps.FillStateTabAsync(data);

            //До сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab("State", false);
            await steps.ClickSaveIncidentAsync();

            //После сохранения
            //Проверяем, что Таба без красной точки
            await steps.VerifyRedDotTab("State", false);
        }

        [Test]
        public async Task StateFieldsCompletenessVerification()
        {
            await steps.FillGeneralTabAsync(data);
            await steps.FillDetailsTabAsync(data);
            await steps.SwitchToTab("State");

            //await steps.VerifyRedDotTab("State", true);

            await steps.VerifyStateTabSpecificLogicAsync();
        }

    }
}

