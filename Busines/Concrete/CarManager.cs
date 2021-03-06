﻿using Busines.Abstract;
using Busines.BusinessAspect.Autofac;
using Busines.Constants;
using Busines.ValidationRules.FluentValidation;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Performance;
using Core.Aspects.Autofac.Transaction;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Validation;
using Core.Utilities.Business;
using Core.Utilities.Results;
using DateAccess.Abstract;
using Entities.Concrete;
using Entities.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Busines.Concrete
{
    public class CarManager : ICarService
    {
        //Bir Entity Manager kendisi hariç başka bir Dal'ı enjekte edemez.
        //Manager sınıflarına ancak servisleri enjekte edebiliriz.

        ICarDal _carDal;
        ICustomerService _customerService;

        public CarManager(ICarDal carDal, ICustomerService customerService)
        {
            _carDal = carDal;
            _customerService = customerService;
        }

        //Business codes CheckIfCarNameExists(car.CarName),

        [CacheRemoveAspect("ICarService.Get")]
        [SecuredOperation("car.add,admin")]
        //[ValidationAspect(typeof(CarValidator))] //Buraya doğrulama için instance değil tipi göndermiş oluyoruz. Başka bir nesnenin yazılmaması için.
        public IResult Add(Car car)
        {
            IResult result = BusinessRules.Run(CheckIfCarCountOfCategoryCorrect(car.CarId),  CheckIfCustomerLimitExiceded());
            if (result != null)
            {
                return result;
            }

            _carDal.Add(car);
            return new SuccessResult(Messages.CarAdded);

            //İlk önce aşağıdaki gibi iş kuralı parçacıklarını yazdık ve çağırdık. Burası ileride çok fazla spagetti olabileceği ce 
            //çirkin kod görünümü ve yönetimi zor ve kötü olacağı için kuralları BusinessRules iş motoruna gönderdik.
            // Hemen üstteki yeni metodla kodlarımızı daha da güzelleştirdik.

            //if (CheckIfCarCountOfCategoryCorrect(car.CarId).Success)
            //{
            //    if (CheckIfCarNameExists(car.CarName).Success)
            //    {
            //        _carDal.Add(car);
            //        return new SuccessResult(Messages.CarAdded);
            //    }
            //}
            //return new ErrorResult(Messages.CarNotAdded);
        }

        //return FluentValidationYaptığımıziçinAşağıdakiMetodaİhtiyacımızKalmadı(car);
        private IResult FluentValidationYaptığımıziçinAşağıdakiMetodaİhtiyacımızKalmadı(Car car)
        {
            // Magic string (" ") yazılan bilgilendirici mesajdır. çok olunca yönetmesi hatalı olur. 
            //Onun için Messages clasından yönetmek için result işlemini yaptık.

            if (car.CarName == null || car.CarName.Length < 2) //eğer aracın isim karakteri 2den küçükse ekleme
            {
                return new ErrorResult(Messages.CarNameInvalid);

            }
            else if (car.DailyPrice <= 0)  // eğer dailyprice 0'dan küçükse ekleme
            {
                return new ErrorResult(Messages.CarDailyPrice);
            }
            else
            {
                _carDal.Add(car);
                return new SuccessResult(Messages.CarAdded);
            }
        }

        public IResult Delete(Car car)
        {
            _carDal.Delete(car);
            return new SuccessResult(Messages.CarDeleted);
        }
        [CacheAspect]
        public IDataResult<List<Car>> GetAll()
        {
            if (DateTime.Now.Hour == 06)
            {
                return new ErrorDataResult<List<Car>>(Messages.MaintenanceTime);
            }
            return new SuccessDataResult<List<Car>>(_carDal.GetAll(), Messages.CarsListed);
        }
        [CacheAspect]
        //[PerformanceAspect(3)]
        public IDataResult<Car> GetByCarId(int categoryId)
        {
            return new SuccessDataResult<Car>(_carDal.Get(c => c.CarId == categoryId), Messages.ListedByCarId);
        }

        public IDataResult<List<CarDetailDto>> GetCarsDetails()
        {
            if (DateTime.Now.Hour == 06)
            {

                return new ErrorDataResult<List<CarDetailDto>>(_carDal.GetCarDetails(), Messages.MaintenanceTime);
            }
            return new SuccessDataResult<List<CarDetailDto>>(_carDal.GetCarDetails(), Messages.CarsDetailListed);
        }

        public IDataResult<List<Car>> GetAllByCarBrand()
        {
            return new SuccessDataResult<List<Car>>(_carDal.GetAll(), Messages.ListedByBrand);
        }

        public IDataResult<List<Car>> GetAllByCarDescription()
        {
            return new SuccessDataResult<List<Car>>(_carDal.GetAll(), Messages.CarsDescriptionListed);
        }

        public IDataResult<List<Car>> GetCarsByBrandId(int brandId)
        {
            return new SuccessDataResult<List<Car>>(_carDal.GetAll(c => c.BrandId == brandId), Messages.ListedByBrandId);
        }

        public IDataResult<List<Car>> GetCarsByColorId(int colorId)
        {
            return new SuccessDataResult<List<Car>>(_carDal.GetAll(c => c.ColorId == colorId), Messages.ListedByColor);
        }

        public IDataResult<List<Car>> GetByCarDailyPrice(decimal min, decimal max)
        {
            return new SuccessDataResult<List<Car>>(_carDal.GetAll(c => c.DailyPrice >= min && c.DailyPrice <= max), Messages.PriceRangeListed);
        }

        [CacheRemoveAspect("ICarService.Get")]
        public IResult Update(Car car)
        {
            _carDal.Update(car);
            return new SuccessResult(Messages.CarUpdated);
        }

        //Bir Kategoriye en fazla 15 nesne eklenebilir.
        private IResult CheckIfCarCountOfCategoryCorrect(int carId)
        {
            var result = _carDal.GetAll(c => c.CarId == carId).Count;
            if (result >= 15)
            {
                return new ErrorResult(Messages.CarCountOfCategoryError);
            }
            return new SuccessResult();
        }

        //Ayni Isimli Araba Eklenemez
        //private IResult CheckIfCarNameExists(string carName)
        //{
        //    var result = _carDal.GetAll(c => c.CarName == carName).Any();
        //    if (result)
        //    {
        //        return new ErrorResult(Messages.CarNameAllreadyExists);
        //    }
        //    return new SuccessResult();
        //}

        //Eğer mevcut kategori sayısı 15'i geçtiryse sisteme yeni ürün eklenemz.
        //Buradaki kural tek başına customer ile alakalı kural değil eğer öyle olsaydı kendi manegarine yazardık.
        // Buradaki durum Car sınıfının customeri nasıl yorumladığı ile alakalıdır.
        // Kural Arabaya bağlı 
        private IResult CheckIfCustomerLimitExiceded()
        {
            var result = _customerService.GetAll();
            if (result.Data.Count > 20)
            {
                return new ErrorResult(Messages.CustomerLimitExiceded);
            }
            return new SuccessResult();
        }


        [TransactionScopeAspect]
        public IResult TransactionalOperation(Car car)
        {
            _carDal.Update(car);
            _carDal.Add(car);
            return new SuccessResult(Messages.CarUpdated);




            //Add(car);
            //if (car.DailyPrice < 100)
            //{
            //    throw new Exception("Günlük ücret 100 ₺ küçük olmamalıdır!");
            //}
            //Add(car);
            //return null;
        }

        public IDataResult<List<Car>> GetCarsByCategoryId(int categoryId)
        {
            return new SuccessDataResult<List<Car>>(_carDal.GetAll(c => c.CategoryId == categoryId), Messages.ListedByCategorydId);
        }

        public IDataResult<List<CarDetailDto>> GetCarsDetailsByBrandId(int brandId)
        {
            return new SuccessDataResult<List<CarDetailDto>>(_carDal.GetCarDetails(c => c.BrandId == brandId), Messages.ListedByBrandId);
        }

        public IDataResult<List<CarDetailDto>> GetCarsDetailsByCategoryId(int categoryId)
        {
            return new SuccessDataResult<List<CarDetailDto>>(_carDal.GetCarDetails(c => c.CategoryId == categoryId), Messages.ListedByCategorydId);
        }

        public IDataResult<List<CarDetailDto>> GetCarsDetailsByColorId(int colorId)
        {
            return new SuccessDataResult<List<CarDetailDto>>(_carDal.GetCarDetails(c => c.ColorId == colorId), Messages.ListedByColorId);

        }

        public IDataResult<List<CarDetailDto>> GetCarsDetailsByCarId(int carId)
        {
            return new SuccessDataResult<List<CarDetailDto>>(_carDal.GetCarDetails(c => c.CarId == carId), Messages.ListedByCarId);

        }

        public IDataResult<List<CarDetailDto>> GetAllCarsDetailsByFilter(int categoryId, int carId, int brandId, int colorId)
        {
            return new SuccessDataResult<List<CarDetailDto>>(_carDal.GetCarDetails()
                .Where(x => x.CategoryId == categoryId && x.CarId == carId && x.BrandId == brandId && x.ColorId == colorId).ToList()
                , Messages.ListedByFilter);
        }
    }
}
