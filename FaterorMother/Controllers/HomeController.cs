using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace LikeParent.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string FaceApiKey = ConfigurationManager.AppSettings["FaceAPIKey"];
        private readonly IFaceServiceClient _faceServiceClient = new FaceServiceClient(FaceApiKey);

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Upload()
        {
            try
            {
                var father = CheckImage("father");
                var mother = CheckImage("mother");
                var child = CheckImage("child");

                var fatherFace = await GetFace(father);
                var motherFace = await GetFace(mother);
                var childFace = await GetFace(child);

                var fatherResult = await GetSimilarity(fatherFace, childFace);
                var motherResult = await GetSimilarity(motherFace, childFace);

                var result = new ResultViewModel
                {
                    Father = new FaceResult
                    {
                        Result = fatherResult,
                        Face = fatherFace
                    },
                    Mother = new FaceResult
                    {
                        Result = motherResult,
                        Face = motherFace
                    },
                    ChildFace = childFace
                };
                return
                    Json(
                        new
                        {
                            success = true,
                            data =
                                string.Format(
                                    "<h2>Congratulations your child is:</h2><h3>{0:P} like the Father</h3><h3>{1:P} like the Mother</h3>",
                                    result.FatherPercent, result.MotherPercent)
                        });
            }
            catch (Exception ex)
            {
                return Json(new {success = false, data = ex.Message});
            }
        }

        private async Task<VerifyResult> GetSimilarity(Face parent, Face child)
        {
            try
            {
                return await _faceServiceClient.VerifyAsync(parent.FaceId, child.FaceId);
            }
            catch (Exception)
            {
                throw new Exception("Oops! Looks like we have an overflow. Please try again later.");
            }
        }

        private async Task<Face> GetFace(Stream image)
        {
            Face[] face;

            try
            {
                face = await _faceServiceClient.DetectAsync(image);
            }
            catch (Exception)
            {
                throw new Exception("Oops! Looks like we have an overflow. Please try again later.");
            }

            if (face == null || !face.Any() || face.Count() > 1)
                throw new Exception("Please upload images with just one face");

            return face.Single();
        }

        private Stream CheckImage(string fileName)
        {
            try
            {
                return Request.Files[fileName].InputStream;
            }
            catch (Exception)
            {
                throw new Exception(string.Format("Please upload {0} image", fileName));
            }
        }
    }

    public class FaceResult
    {
        public VerifyResult Result { get; set; }
        public Face Face { get; set; }
    }

    public class ResultViewModel
    {
        public FaceResult Father { get; set; }
        public FaceResult Mother { get; set; }
        public Face ChildFace { get; set; }

        public double FatherPercent
        {
            get
            {
                var totalConfidence = Father.Result.Confidence + Mother.Result.Confidence;
                return Father.Result.Confidence/totalConfidence;
            }
        }

        public double MotherPercent
        {
            get { return 1 - FatherPercent; }
        }
    }
}