using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FoodDataCentral.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FoodDataCentral.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly FoodDataCentralApplicationDbContext db;

        //Base URL for the IEXTrading API. Method specific URLs are appended to this base URL.
        string BASE_URL = "https://api.nal.usda.gov/fdc/";
        private string API_KEY = "kfHUyRHaIdM9GDcnhA0sQffBGi2UirR6oH6LmOpg";
        HttpClient httpClient;

        public HomeController(ILogger<HomeController> logger, FoodDataCentralApplicationDbContext dbObjectReference)
        {
            _logger = logger;
            db = dbObjectReference;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new
                System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult FoodSelection()
        {
            Root passedModel = new Root();
            passedModel.foods = new Food();
            passedModel.QueryInput = "";
            return View(passedModel);
        }

        public IActionResult FoodRead()
        {
            Root passedModel = new Root();
            passedModel.foods = new Food();
            passedModel.QueryInput = "";
            return View(passedModel);
        }

        public IActionResult GetFoodDataFromApi(string queryInput)
        {
            Random rnd = new Random();
            dynamic model = new ExpandoObject();
            string data = "";
            List<Food> foodList = null;

            int page = rnd.Next(1, 5);
            string API_PATH = BASE_URL + "v1/foods/search?query=" + queryInput + "&pageSize=10&pageNumber=" + page +
                              "&api_key=" + API_KEY;

            HttpResponseMessage response = httpClient.GetAsync(API_PATH).GetAwaiter().GetResult();


            if (response.IsSuccessStatusCode)
            {
                data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var jsonDocument = JsonDocument.Parse(data);
                var foodArray = jsonDocument.RootElement.GetProperty("foods");
                foodList = JsonConvert.DeserializeObject<List<Food>>(foodArray.ToString());
                _logger.LogInformation(foodArray.ToString());
            }

           
            Root passedModel = new Root();
            passedModel.foods = foodList[0];
            passedModel.QueryInput = queryInput;

            Food food = new Food();
            

            food.fdcId = passedModel.foods.fdcId;
            food.description = passedModel.foods.description;
            food.brandName = passedModel.foods.brandName;
            food.brandOwner = passedModel.foods.brandOwner;
            food.dataType = passedModel.foods.dataType;
            food.marketCountry = passedModel.foods.marketCountry;
            food.ingredients = passedModel.foods.ingredients;
            food.publishedDate = passedModel.foods.publishedDate;
            food.foodNutrients = new List<FoodNutrient>();

            foreach (var foodNut in passedModel.foods.foodNutrients)
            {
                FoodNutrient fNut = new FoodNutrient();
                fNut.nutrientId = foodNut.nutrientId;
                fNut.nutrientName = foodNut.nutrientName;
                fNut.nutrientNumber = foodNut.nutrientNumber;
                fNut.unitName = foodNut.unitName;
                fNut.value = foodNut.value;
                food.foodNutrients.Add(fNut);
            }

            db.Foods.Add(food);
            db.SaveChanges();

            Root passedModel2 = new Root();
            passedModel2.foods = food;
            passedModel2.QueryInput = queryInput;

            return View("FoodSelection", passedModel2);
        }

        public IActionResult GetFoodDataFromDb(string queryInput)
        {
            _logger.LogInformation(queryInput);
            List<Food> foodList = null;
            Food food = db.Foods.Include(p => p.foodNutrients).Where(row => row.fdcId == Convert.ToInt32(queryInput)).First();

            Root passedModel = new Root();
            passedModel.foods = food;
            passedModel.QueryInput = queryInput;

            return View("FoodRead", passedModel);
        }


        /*[HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create()
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }*/


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}