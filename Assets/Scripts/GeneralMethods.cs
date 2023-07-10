using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChemistryLab
{
    public static class APIHandler
    {
        public static async Task<List<ResponseObject>> FetchExperimentData()
        {
            HttpClient client = new HttpClient();
            List<ResponseObject> responseObjects = new List<ResponseObject>();

            try
            {
                HttpResponseMessage response = await client.GetAsync("http://24.199.120.99/api/experiments/");

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Deserialize JSON response into a list of objects
                    responseObjects = JsonConvert.DeserializeObject<List<ResponseObject>>(jsonResponse);
                }
                else
                {
                    Console.WriteLine("Error occurred: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Dispose of the HttpClient instance when done
                client.Dispose();
            }

            return responseObjects;
        }
    }

    public class ResponseObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Equipment> Equipments { get; set; }
        public List<Chemical> Chemicals { get; set; }
        public List<Step> Steps { get; set; }
        public string Observation { get; set; }
        public string Conclusion { get; set; }
    }

    public class Equipment
    {
        public string Name { get; set; }
    }

    public class Chemical
    {
        public string Name { get; set; }
    }

    public class Step
    {
        public string Verb { get; set; }
        public string Equipment { get; set; }
        public string Chemical { get; set; }
        public string Quantity { get; set; }
    }

    public class GeneralMethods : MonoBehaviour
    {
        public Text instructionText;
        public static GeneralMethods sing;//singelton pattern
        public SkinnedMeshRenderer Pipe2FutureStarchSkRndrer;


        //observer pattern
        public delegate void MyObserverDelegete();
        public MyObserverDelegete myMissionEventObserver;
        public MyObserverDelegete myAlertsEventObserver;
        public MyObserverDelegete ResetNowObserver;

        public bool TimerEnabled = false;
        private float startWatch;
        bool TimeEnds = false;
        public void EnableTimer()
        {
            if (toWaitTime != 12)
            {
                if (ParameterFixingGlitchWaitTime == 0)
                {
                    UIObjsHandler.instance.AlertString = "Wrong , Set Timer time first";
                }
                else
                {
                    UIObjsHandler.instance.AlertString = "Wrong timer set , correct it with 120";
                }

                return;
            }

            if (TasksManager.MissionID < 7)
            {
                UIObjsHandler.instance.AlertString = "Finish previous nessecessary tasks first";
                return;
            }

            if (TasksManager.MissionID == 7)
            {

                if (FireBunsenHandler.singelton.myfireState != FireBunsenHandler.FireState.InFire)
                {
                    UIObjsHandler.instance.AlertString = "Kidding? Turn The Fire On First";
                    return;
                }
            }

            TimeEnds = false;
            TimerEnabled = true;
            startWatch = Time.time;
        }



        float ParameterFixingGlitchWaitTime = 0;//if using toWaitTime directly ,, (out) will make a glitch where any num is accepted while Timer already running
        public void OnEndEdit(string time)
        {
            if (float.TryParse(time, out ParameterFixingGlitchWaitTime))
            {
                if (ParameterFixingGlitchWaitTime != 120)
                {
                    UIObjsHandler.instance.AlertString = "Wrong , 2 minutes required";

                }
                else
                {
                    toWaitTime = ParameterFixingGlitchWaitTime / 10; //faking the time for not let user wait so long
                }
            }
            else
            {
                UIObjsHandler.instance.AlertString = "Wrong , Enter valid seconds";
            }

        }
        float spentTime;
        float toWaitTime = 0;
        private void Update()
        {
            if (TimerEnabled)
            {
                spentTime = Time.time - startWatch;
                if (spentTime >= toWaitTime)
                {
                    TimeEnds = true;
                    TimerEnabled = false;//must 

                    if (TasksManager.MissionID == 7)
                    {
                        //change nasha color to Purble now
                        if (Pipe2FutureStarchSkRndrer)
                            Pipe2FutureStarchSkRndrer.material.color = new Color32(143, 0, 254, 255);

                        TasksManager.MissionID++;
                    }
                }
                else
                {
                    UIObjsHandler.instance.TimerTxt.text = (spentTime * 10f).ToString();//faking 12 seconds to be 120
                }

            }
        }


        public void OnValueChanged(float val)
        {
            UIObjsHandler.instance.amountofSolInputSTR = val.ToString(); // this is a proberty that affects mutlible elemnts
        }


        public void OnValueChanged(Int32 chosenlang)
        {
            SetEnglishInstructions();
        }

        void SetEnglishInstructions()
        {
            if (instructionText)
            {
                instructionText.alignment = TextAnchor.MiddleLeft;
                instructionText.lineSpacing = 1;
            }
        }


        private async void Awake()
        {
            sing = this;

            // Fetch experiment data from the API
            List<ResponseObject> responseObjects = await APIHandler.FetchExperimentData();

            if (responseObjects.Count > 0)
            {
                ResponseObject firstObject = responseObjects[0];
                List<Equipment> equipments = firstObject.Equipments;
                List<Step> steps = firstObject.Steps;
                string observation = firstObject.Observation;
                string conclusion = firstObject.Conclusion;

                // Set the instructions based on the fetched data
                TheInstructions = new string[steps.Count + 4];
                SetEnglishInstructions();
                string all_equipment = "";
                for(int i = 0; i < equipments.Count; i++)
                {
                    all_equipment += equipments[i].Name + ", ";
                }
                TheInstructions[0] = " ";
                TheInstructions[1] = "See safety instructions (click the button at the top)";
                
                for (int i = 0; i < steps.Count; i++)
                {
                    if(steps[i].Verb == "Add"){
                        steps[i].Quantity = " " + steps[i].Quantity + " ml from ";
                        steps[i].Equipment = " to " + steps[i].Equipment;
                    }
                    else if(steps[i].Verb == "Light"){
                        steps[i].Verb = "Light the ";
                        steps[i].Chemical = "";
                        steps[i].Quantity = "";
                    }
                    else if(steps[i].Verb == "Boil"){
                        steps[i].Verb = "Boil ";
                        steps[i].Equipment = "";
                        steps[i].Quantity = "";
                    }

                    if(i == 3){
                        TheInstructions[3] = "Tools Needed: "+ all_equipment+"\n--------\n "+steps[i-2].Verb + steps[i-2].Quantity + steps[i-2].Chemical + steps[i-2].Equipment;
                    }
                    
                    TheInstructions[i+2] = steps[i].Verb + steps[i].Quantity + steps[i].Chemical + steps[i].Equipment;
                }

                int x = steps.Count+2;
                TheInstructions[x] = "Observation: "+observation;
                TheInstructions[x+1] = "Conclusion: "+conclusion;
            }
            
        }

        void Start()
        {
            ResetNowObserver += ResetGeneral;
            //missionPassed observer
            myMissionEventObserver += GoToMissionInstruction;
        }
        public string[] TheInstructions;

        public void NextInstruction()
        {
            UIObjsHandler.instance.StepTxtDeal++;
            UIObjsHandler.instance.TxtOfInstr = TheInstructions[UIObjsHandler.instance.StepTxtDeal];
        }
        public void GoToMissionInstruction()
        {
            UIObjsHandler.instance.StepTxtDeal = TasksManager.MissionID;
            UIObjsHandler.instance.TxtOfInstr = TheInstructions[TasksManager.MissionID];
        }
        public void PrevInstruction()
        {
            UIObjsHandler.instance.StepTxtDeal--;
            UIObjsHandler.instance.TxtOfInstr = TheInstructions[UIObjsHandler.instance.StepTxtDeal];
        }
        public void ToggleInstructionPanel()
        {
            UIObjsHandler.instance.InstructionPanel.SetActive(!UIObjsHandler.instance.InstructionPanel.activeInHierarchy);
            UIObjsHandler.instance.ChooseAmountPanel.SetActive(!UIObjsHandler.instance.ChooseAmountPanel.activeInHierarchy);

        }
        public void ResetGeneral()
        {
            TimerEnabled = false;
            toWaitTime = 0;
            UIObjsHandler.instance.TimerTxt.text = "0.0";
        }
        private void OnDisable()
        {
            myMissionEventObserver -= GoToMissionInstruction;
            ResetNowObserver -= ResetGeneral;
        }
    }
}