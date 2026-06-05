# MiniCommerce — Session 01 Ders Notları

## Konu

Bu doküman, MiniCommerce mikroservis eğitim projesinin ilk milestone notudur.

Bu session'da aşağıdaki kapsam tamamlandı:

- WSL2 Ubuntu ortam kontrolü
- Solution ve klasör yapısı
- Docker Compose altyapısı
- MongoDB, Redis, PostgreSQL, RabbitMQ container'ları
- `MiniCommerce.Shared` projesi
- `Error` ve `ServiceResult` response pattern
- `MiniCommerce.Catalog.API`
- MongoDB bağlantısı
- Product CRUD
- DTO kullanımı
- Repository pattern
- Service layer
- Validation
- ServiceResult -> HTTP response mapping
- Global exception handling
- MongoDB health check
- İlk Git commit

Bu doküman junior–mid level geliştiricilere anlatım için hazırlanmıştır. Sadece kodu değil, kodun neden bu sırayla yazıldığını ve hangi mimari kararı temsil ettiğini de açıklar.

---

# 1. Genel Proje Hedefi

MiniCommerce, .NET 9 ile geliştirilen gerçekçi ama öğrenilebilir bir mikroservis eğitim projesidir.

Ana hedefler:

- Mikroservis mimarisini uygulamalı göstermek
- Database per service yaklaşımını öğretmek
- Service-to-service HTTP communication göstermek
- RabbitMQ + MassTransit ile messaging öğretmek
- Eventual consistency kavramını göstermek
- API Gateway / YARP kullanımını eklemek
- Docker Compose ile altyapı yönetimini öğretmek
- Production'a yakın ama aşırı karmaşık olmayan bir yapı kurmak

Kapsam dışı bırakılanlar:

- Identity.API
- JWT Authentication
- ASP.NET Identity
- Role / Authorization
- Saga State Machine

Authentication bu projede bilinçli olarak çıkarıldı. Basket ve Order işlemlerinde ileride `X-Customer-Id` header'ı ile demo customer scope gösterilecek.

---

# 2. Session 01 Kapsamı

Bu session sonunda çalışan ilk servisimiz hazırlandı:

```text
MiniCommerce.Catalog.API
```

Catalog servisi şu anda:

- Product oluşturabiliyor
- Product listeleyebiliyor
- Product detay getirebiliyor
- Product güncelleyebiliyor
- Product silebiliyor
- MongoDB kullanıyor
- Validation yapıyor
- ServiceResult pattern kullanıyor
- Global exception handler kullanıyor
- MongoDB health check yapıyor

Henüz eklenmeyenler:

- Basket.API
- Order.API
- MassTransit
- RabbitMQ consumer/publisher
- Stock reservation
- Atomic stock update
- API Gateway
- Dockerized API services
- Outbox Pattern

Bunlar ihtiyaç ortaya çıktığında sırayla eklenecek.

---

# 3. Ortam Kontrolü

İlk adımda geliştirme ortamı kontrol edildi.

Kullanılan komutlar:

```bash
pwd
dotnet --version
dotnet --list-sdks
docker ps
docker compose version
```

Alınan sonuç:

```text
/home/hmztpl
9.0.116
9.0.116 [/usr/lib/dotnet/sdk]
Docker Compose version v5.1.4
```

## Neden ortam kontrolü yaptık?

Bir mikroservis projesinde hata sadece koddan kaynaklanmaz. SDK versiyonu, Docker durumu, port çakışmaları veya Compose versiyonu da problem çıkarabilir.

Bu yüzden kod yazmadan önce temel ortam doğrulandı.

## Production'da daha gelişmiş hali ne olurdu?

Gerçek ekiplerde ortam doğrulama genelde şu yapılarla desteklenir:

- devcontainer
- setup script
- Makefile
- bootstrap script
- CI environment check
- dependency version locking

---

# 4. Solution Yapısı

Hedef solution yapısı:

```text
MiniCommerce/
├── MiniCommerce.sln
├── docker-compose.yml
├── docs/
├── postman/
└── src/
    ├── shared/
    │   └── MiniCommerce.Shared/
    └── services/
        ├── catalog/
        │   └── MiniCommerce.Catalog.API/
        ├── basket/
        ├── order/
        └── gateway/
```

## Neden bu yapı?

Mikroservis projelerinde fiziksel klasör ayrımı önemlidir.

Bu yapı sayesinde:

- Shared proje ayrı durur.
- Her servis kendi klasöründe gelişir.
- Gateway ayrı servis olarak konumlanır.
- Postman collection ayrı yönetilir.
- Ders notları `docs` altında tutulur.

## Oluşturma komutları

```bash
mkdir -p ~/projects/MiniCommerce
cd ~/projects/MiniCommerce

dotnet new sln -n MiniCommerce

mkdir -p src/shared
mkdir -p src/services/catalog
mkdir -p src/services/basket
mkdir -p src/services/order
mkdir -p src/services/gateway
mkdir -p postman

find . -maxdepth 4 -type d | sort
```

---

# 5. Shared Library

Shared proje oluşturuldu:

```bash
dotnet new classlib \
  -n MiniCommerce.Shared \
  -o src/shared/MiniCommerce.Shared \
  -f net9.0

dotnet sln MiniCommerce.sln add src/shared/MiniCommerce.Shared/MiniCommerce.Shared.csproj
```

## Shared library'nin amacı

`MiniCommerce.Shared`, servisler arasında ortak kullanılan yapıları tutar.

Bu projede şu tipler burada yer alacak:

- Common result modelleri
- Error modeli
- Message contract'ları
- Ortak DTO'lar
- Constants

## Kritik mimari karar

Tüm command/event contract'ları baştan oluşturulmadı.

Örneğin şu contract'ları henüz yazmadık:

- `SubmitOrderCommand`
- `ReserveStockCommand`
- `StockReservedEvent`
- `StockReservationFailedEvent`
- `OrderCreatedEvent`
- `OrderRejectedEvent`

Bunun nedeni eğitim açısından önemlidir.

Junior–mid ekip için önce problem görünmeli, sonra contract ihtiyacı doğmalıdır. Aksi halde command/event yapıları soyut kalır.

## Shared klasörleri

```bash
rm -f src/shared/MiniCommerce.Shared/Class1.cs

mkdir -p src/shared/MiniCommerce.Shared/Common
mkdir -p src/shared/MiniCommerce.Shared/Messaging/Commands
mkdir -p src/shared/MiniCommerce.Shared/Messaging/Events
mkdir -p src/shared/MiniCommerce.Shared/Constants
mkdir -p src/shared/MiniCommerce.Shared/DTOs
```

---

# 6. Docker Compose Altyapısı

Aşağıdaki altyapı servisleri Docker Compose ile tanımlandı:

| Servis | Amaç | Host Port |
|---|---|---|
| MongoDB | Catalog verisi | 27031 |
| Mongo Express | MongoDB UI | 27032 |
| Redis | Basket verisi | 6380 |
| PostgreSQL | Order verisi | 5433 |
| RabbitMQ | Message broker | 5673 |
| RabbitMQ Management UI | RabbitMQ yönetim ekranı | 15673 |

## docker-compose.yml

```yaml
services:
  minicomm.rabbitmq:
    image: rabbitmq:3-management
    container_name: minicomm.rabbitmq
    ports:
      - "5673:5672"
      - "15673:15672"
    environment:
      RABBITMQ_DEFAULT_USER: minicomm
      RABBITMQ_DEFAULT_PASS: minicomm123
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    networks:
      - minicomm-network
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "check_running"]
      interval: 10s
      timeout: 5s
      retries: 5

  minicomm.mongodb:
    image: mongo:7
    container_name: minicomm.mongodb
    ports:
      - "27031:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: minicomm
      MONGO_INITDB_ROOT_PASSWORD: minicomm123
    volumes:
      - mongodb_data:/data/db
    networks:
      - minicomm-network
    healthcheck:
      test: ["CMD", "mongosh", "--quiet", "--eval", "db.adminCommand('ping').ok"]
      interval: 10s
      timeout: 5s
      retries: 5

  minicomm.mongo-express:
    image: mongo-express:1.0.2
    container_name: minicomm.mongo-express
    ports:
      - "27032:8081"
    environment:
      ME_CONFIG_MONGODB_ADMINUSERNAME: minicomm
      ME_CONFIG_MONGODB_ADMINPASSWORD: minicomm123
      ME_CONFIG_MONGODB_URL: mongodb://minicomm:minicomm123@minicomm.mongodb:27017/
      ME_CONFIG_BASICAUTH_USERNAME: admin
      ME_CONFIG_BASICAUTH_PASSWORD: admin123
    depends_on:
      minicomm.mongodb:
        condition: service_healthy
    networks:
      - minicomm-network

  minicomm.redis:
    image: redis:7
    container_name: minicomm.redis
    ports:
      - "6380:6379"
    volumes:
      - redis_data:/data
    networks:
      - minicomm-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  minicomm.postgres:
    image: postgres:16
    container_name: minicomm.postgres
    ports:
      - "5433:5432"
    environment:
      POSTGRES_USER: minicomm
      POSTGRES_PASSWORD: minicomm123
      POSTGRES_DB: orderdb
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - minicomm-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U minicomm -d orderdb"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  rabbitmq_data:
  mongodb_data:
  redis_data:
  postgres_data:

networks:
  minicomm-network:
    driver: bridge
```

## Neden API'leri şimdilik Docker'da çalıştırmadık?

Bu aşamada API'ler `dotnet run` ile local çalıştırılıyor.

Sebep:

- Debugging daha kolay
- Kod değişiklikleri hızlı test edilir
- Junior ekip için akış daha görünür olur
- Önce servis iç yapısını öğreniyoruz

Sonraki aşamada API'ler de Dockerize edilecek.

---

# 7. Container'ları Ayağa Kaldırma

Komutlar:

```bash
docker compose up -d
docker compose ps
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

Beklenen servisler:

```text
minicomm.mongo-express
minicomm.mongodb
minicomm.postgres
minicomm.rabbitmq
minicomm.redis
```

RabbitMQ UI:

```text
http://localhost:15673
username: minicomm
password: minicomm123
```

Mongo Express UI:

```text
http://localhost:27032
username: admin
password: admin123
```

Mongo Express için `401 Unauthorized` görülmesi normaldir. Basic authentication bilinçli olarak açılmıştır.

---

# 8. Error Modeli

Dosya:

```text
src/shared/MiniCommerce.Shared/Common/Error.cs
```

Kod:

```csharp
namespace MiniCommerce.Shared.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new("None", string.Empty);

    public static Error Validation(string message = "One or more validation errors occurred.")
        => new("Validation.Error", message);
}
```

## Ne işe yarar?

`Error`, sistemdeki başarısızlıkları standart temsil etmek için kullanılır.

Örnek hata kodları:

```text
Product.NotFound
Product.InvalidId
Basket.Empty
Stock.Insufficient
Validation.Error
```

## Neden sadece string mesaj dönmüyoruz?

Sadece string mesaj client tarafında güvenilir şekilde işlenemez.

Kötü response:

```json
{
  "message": "Product was not found."
}
```

Daha iyi response:

```json
{
  "error": {
    "code": "Product.NotFound",
    "message": "Product was not found."
  }
}
```

`Code` alanı frontend, API client veya log analizinde hatayı sınıflandırmak için kullanılır.

## `Error.None` neden var?

Başarılı sonuçlarda hata olmamalıdır.

`Error.None`, `null` yerine "hata yok" durumunu temsil eder.

## `Error.Validation()` neden var?

Validation hataları çok sık kullanılacağı için standart factory method olarak eklendi.

Böylece her yerde `"Validation.Error"` string'i tekrar edilmez.

---

# 9. ServiceResult Pattern

Dosya:

```text
src/shared/MiniCommerce.Shared/Common/ServiceResult.cs
```

Kod:

```csharp
namespace MiniCommerce.Shared.Common;

public class ServiceResult
{
    protected ServiceResult(
        bool isSuccess,
        Error error,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None && validationErrors is null)
        {
            throw new InvalidOperationException("Failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public static ServiceResult Success()
        => new(true, Error.None);

    public static ServiceResult Fail(Error error)
        => new(false, error);

    public static ServiceResult ValidationFail(IReadOnlyDictionary<string, string[]> validationErrors)
        => new(false, Error.Validation(), validationErrors);
}

public class ServiceResult<T> : ServiceResult
{
    private ServiceResult(
        bool isSuccess,
        T? data,
        Error error,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, error, validationErrors)
    {
        Data = data;
    }

    public T? Data { get; }

    public static ServiceResult<T> Success(T data)
        => new(true, data, Error.None);

    public new static ServiceResult<T> Fail(Error error)
        => new(false, default, error);

    public new static ServiceResult<T> ValidationFail(IReadOnlyDictionary<string, string[]> validationErrors)
        => new(false, default, Error.Validation(), validationErrors);
}
```

## Ne işe yarar?

`ServiceResult`, service layer'dan endpoint'e dönen operasyon sonucunu standartlaştırır.

Bir operasyon şu sonuçlardan birini dönebilir:

- Başarılı ve data var
- Başarılı ama data yok
- Business error
- Validation error

Örnek:

```csharp
return ServiceResult<ProductResponse>.Success(product);
```

veya:

```csharp
return ServiceResult<ProductResponse>.Fail(
    new Error("Product.NotFound", "Product was not found."));
```

## Neden doğrudan DTO veya null dönmedik?

Kötü yaklaşım:

```csharp
ProductResponse? GetProductById(string id)
```

Bu durumda `null` belirsizdir.

`null` şu anlama gelebilir:

- Product yok
- Validation hatası var
- Database hatası oldu
- Mapping hatası oldu

`ServiceResult` ile hata nedeni açık hale gelir.

## Exception yerine mi geçiyor?

Hayır.

Ayrım:

| Durum | Yaklaşım |
|---|---|
| Product bulunamadı | ServiceResult.Fail |
| Validation hatası | ServiceResult.ValidationFail |
| Basket boş | ServiceResult.Fail |
| Database bağlantısı koptu | Exception |
| Beklenmeyen bug | Exception |

Business durumları result ile, beklenmeyen teknik hatalar exception ile yönetilir.

---

# 10. Catalog.API Oluşturma

Komutlar:

```bash
dotnet new webapi \
  -n MiniCommerce.Catalog.API \
  -o src/services/catalog/MiniCommerce.Catalog.API \
  -f net9.0

dotnet sln MiniCommerce.sln add src/services/catalog/MiniCommerce.Catalog.API/MiniCommerce.Catalog.API.csproj

dotnet add src/services/catalog/MiniCommerce.Catalog.API/MiniCommerce.Catalog.API.csproj \
  reference src/shared/MiniCommerce.Shared/MiniCommerce.Shared.csproj
```

Klasörler:

```bash
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Endpoints
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/DTOs
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Services
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Repositories
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Options
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Extensions
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Exceptions
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Consumers
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Data
mkdir -p src/services/catalog/MiniCommerce.Catalog.API/Entities
```

## Neden klasörleri baştan açtık?

Her klasör bir sorumluluk alanını temsil eder.

| Klasör | Sorumluluk |
|---|---|
| Endpoints | Minimal API endpoint mapping |
| DTOs | Request/response modelleri |
| Services | Business/application logic |
| Repositories | Data access abstraction |
| Options | Strongly typed configuration |
| Extensions | DI ve middleware extension'ları |
| Exceptions | Global exception handling |
| Consumers | İleride MassTransit consumer'ları |
| Data | Database context/setup |
| Entities | Database modelleri |
| HealthChecks | Dependency health check'leri |

Her servis tüm klasörleri kullanmak zorunda değildir. İhtiyaç oldukça kullanılır.

---

# 11. MongoDB Driver Paketi

Komut:

```bash
dotnet add src/services/catalog/MiniCommerce.Catalog.API/MiniCommerce.Catalog.API.csproj \
  package MongoDB.Driver
```

Bu paket MongoDB ile .NET üzerinden iletişim kurmak için kullanılır.

---

# 12. MongoDbOptions

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Options/MongoDbOptions.cs
```

Kod:

```csharp
namespace MiniCommerce.Catalog.API.Options;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = string.Empty;

    public string ProductsCollectionName { get; init; } = "products";
}
```

## Açıklama

Bu class, `appsettings.json` içindeki şu section'ı strongly typed hale getirir:

```json
"MongoDb": {
  "ConnectionString": "mongodb://minicomm:minicomm123@localhost:27031/?authSource=admin",
  "DatabaseName": "catalogdb",
  "ProductsCollectionName": "products"
}
```

## Neden Options Pattern?

Kötü yaklaşım:

```csharp
var connectionString = configuration["MongoDb:ConnectionString"];
```

Daha iyi yaklaşım:

```csharp
IOptions<MongoDbOptions>
```

Avantajlar:

- Magic string azalır
- Configuration merkezi olur
- Validation yapılabilir
- Test edilebilirlik artar
- Runtime config hataları daha erken yakalanır

---

# 13. CatalogInfrastructureExtensions

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Extensions/CatalogInfrastructureExtensions.cs
```

Kod:

```csharp
using Microsoft.Extensions.Options;
using MiniCommerce.Catalog.API.Options;
using MiniCommerce.Catalog.API.Repositories;
using MiniCommerce.Catalog.API.Services;
using MongoDB.Driver;

namespace MiniCommerce.Catalog.API.Extensions;

public static class CatalogInfrastructureExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "MongoDb:ConnectionString is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                "MongoDb:DatabaseName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ProductsCollectionName),
                "MongoDb:ProductsCollectionName is required.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MongoDbOptions>>()
                .Value;

            return new MongoClient(options.ConnectionString);
        });

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MongoDbOptions>>()
                .Value;

            var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();

            return mongoClient.GetDatabase(options.DatabaseName);
        });

        services.AddScoped<IProductRepository, MongoProductRepository>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
```

## Ne işe yarıyor?

Catalog servisinin infrastructure bağımlılıklarını DI container'a ekler:

- `MongoDbOptions`
- `IMongoClient`
- `IMongoDatabase`
- `IProductRepository`
- `IProductService`

## Neden extension method?

`Program.cs` dosyasının şişmesini engeller.

Kötü yaklaşım:

```csharp
builder.Services.AddOptions...
builder.Services.AddSingleton...
builder.Services.AddScoped...
```

Daha iyi yaklaşım:

```csharp
builder.Services.AddCatalogInfrastructure(builder.Configuration);
```

## `ValidateOnStart()` neden önemli?

Konfigürasyon hatasını uygulama başında yakalar.

Bu yaklaşıma fail-fast denir.

Hatalı MongoDB connection string ile uygulamanın yarım çalışması yerine, başta patlaması daha doğru ve debug edilebilirdir.

## `MongoClient` neden singleton?

MongoDB driver'daki `MongoClient` thread-safe'tir ve tekrar tekrar oluşturulmamalıdır.

Doğru yaklaşım:

```text
Uygulama boyunca tek MongoClient instance'ı reuse edilir.
```

Yanlış yaklaşım:

```csharp
new MongoClient(...)
```

bunu her request'te yapmak.

---

# 14. appsettings.json

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/appsettings.json
```

Kod:

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://minicomm:minicomm123@localhost:27031/?authSource=admin",
    "DatabaseName": "catalogdb",
    "ProductsCollectionName": "products"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Açıklama

`localhost:27031`, Docker Compose port mapping nedeniyle kullanılır:

```yaml
27031:27017
```

Container içinde MongoDB `27017` portunda çalışır. Host/WSL tarafından `27031` üzerinden erişilir.

`authSource=admin`, MongoDB root user'ı `admin` database üzerinde oluşturulduğu için gereklidir.

---

# 15. Product Entity

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Entities/Product.cs
```

Kod:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MiniCommerce.Catalog.API.Entities;

public sealed class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("stock")]
    public int Stock { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
```

## Entity nedir?

Entity, database'de saklanan modeli temsil eder.

Burada `Product`, MongoDB dokümanının C# karşılığıdır.

## `[BsonId]` ne işe yarar?

MongoDB'deki `_id` alanını temsil eder.

## `[BsonRepresentation(BsonType.ObjectId)]` neden var?

MongoDB `_id` değerini ObjectId olarak tutar. C# tarafında bunu string olarak kullanmak API açısından daha pratiktir.

## `[BsonElement("name")]` ne işe yarar?

C# property adı `Name`, MongoDB alan adı `name` olur.

Bu sayede MongoDB dokümanları camelCase tutulur.

## Neden entity'yi doğrudan response olarak dönmüyoruz?

Entity database modelidir. API response modeli değildir.

Doğru ayrım:

```text
Entity = Database modeli
DTO = API contract modeli
```

---

# 16. Product DTO'ları

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/DTOs/ProductDtos.cs
```

Kod:

```csharp
namespace MiniCommerce.Catalog.API.DTOs;

public sealed record ProductResponse(
    string Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock);

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock);
```

## DTO nedir?

DTO, Data Transfer Object anlamına gelir.

API'nin dış dünyayla konuştuğu request/response modelleridir.

## Neden record kullandık?

Bu modeller data taşıdığı için `record` sade ve okunabilir bir tercihtir.

## Neden entity'den ayrı?

İleride entity'ye internal alanlar eklenebilir:

- `SupplierId`
- `InternalCost`
- `IsDeleted`
- `ReservedStock`

Bu alanları API response olarak dönmek istemeyebiliriz.

DTO ayrımı bu kontrolü sağlar.

---

# 17. IProductRepository

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Repositories/IProductRepository.cs
```

Kod:

```csharp
using MiniCommerce.Catalog.API.Entities;

namespace MiniCommerce.Catalog.API.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task CreateAsync(Product product, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
```

## Repository interface ne işe yarar?

Data erişiminin contract'ını belirler.

Service layer MongoDB driver detaylarını bilmez.

Service sadece interface ile konuşur.

## Neden önemli?

Bu ayrım test edilebilirlik sağlar.

Örneğin unit testlerde gerçek MongoDB yerine fake repository kullanılabilir.

---

# 18. MongoProductRepository

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Repositories/MongoProductRepository.cs
```

Kod:

```csharp
using Microsoft.Extensions.Options;
using MiniCommerce.Catalog.API.Entities;
using MiniCommerce.Catalog.API.Options;
using MongoDB.Driver;

namespace MiniCommerce.Catalog.API.Repositories;

public sealed class MongoProductRepository : IProductRepository
{
    private readonly IMongoCollection<Product> _products;

    public MongoProductRepository(
        IMongoDatabase database,
        IOptions<MongoDbOptions> options)
    {
        _products = database.GetCollection<Product>(
            options.Value.ProductsCollectionName);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _products
            .Find(_ => true)
            .SortByDescending(product => product.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return await _products
            .Find(product => product.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(Product product, CancellationToken cancellationToken)
    {
        await _products.InsertOneAsync(product, cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        product.UpdatedAt = DateTime.UtcNow;

        var result = await _products.ReplaceOneAsync(
            existingProduct => existingProduct.Id == product.Id,
            product,
            cancellationToken: cancellationToken);

        return result.ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _products.DeleteOneAsync(
            product => product.Id == id,
            cancellationToken);

        return result.DeletedCount == 1;
    }
}
```

## Ne işe yarar?

`IProductRepository` interface'inin MongoDB implementasyonudur.

## `GetAllAsync`

```csharp
.Find(_ => true)
.SortByDescending(product => product.CreatedAt)
```

Tüm ürünleri getirir ve en yeni ürünü üstte listeler.

## `GetByIdAsync`

Id'ye göre product arar. Bulamazsa `null` döner.

Bu bir exception değildir. Beklenen business durumudur.

## `CreateAsync`

Product dokümanını MongoDB'ye ekler.

## `UpdateAsync`

Product dokümanını replace eder.

`ModifiedCount == 1` ise update başarılı kabul edilir.

## `DeleteAsync`

Product silinirse `true`, bulunamazsa `false` döner.

## CancellationToken neden var?

HTTP request iptal edilirse veya timeout olursa alttaki MongoDB operasyonu da iptal edilebilir.

Bu production sistemlerde gereksiz iş yükünü azaltır.

---

# 19. IProductService

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Services/IProductService.cs
```

Kod:

```csharp
using MiniCommerce.Catalog.API.DTOs;
using MiniCommerce.Shared.Common;

namespace MiniCommerce.Catalog.API.Services;

public interface IProductService
{
    Task<ServiceResult<IReadOnlyList<ProductResponse>>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ServiceResult<ProductResponse>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProductResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProductResponse>> UpdateAsync(
        string id,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken);
}
```

## Service interface ne işe yarar?

Catalog servisinin application operasyonlarını tanımlar.

Endpoint tarafı repository'yi doğrudan çağırmaz.

Doğru akış:

```text
Endpoint → ProductService → ProductRepository → MongoDB
```

---

# 20. ProductService

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Services/ProductService.cs
```

Kod:

```csharp
using MiniCommerce.Catalog.API.DTOs;
using MiniCommerce.Catalog.API.Entities;
using MiniCommerce.Catalog.API.Repositories;
using MiniCommerce.Shared.Common;

namespace MiniCommerce.Catalog.API.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IReadOnlyList<ProductResponse>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);

        var response = products
            .Select(MapToResponse)
            .ToList();

        return ServiceResult<IReadOnlyList<ProductResponse>>.Success(response);
    }

    public async Task<ServiceResult<ProductResponse>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ServiceResult<ProductResponse>.Fail(
                new Error("Product.InvalidId", "Product id is required."));
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ServiceResult<ProductResponse>.Fail(
                new Error("Product.NotFound", "Product was not found."));
        }

        return ServiceResult<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<ServiceResult<ProductResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateProductInput(
            request.Name,
            request.Description,
            request.Price,
            request.Stock);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<ProductResponse>.ValidationFail(validationErrors);
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.CreateAsync(product, cancellationToken);

        _logger.LogInformation("Product created. ProductId: {ProductId}", product.Id);

        return ServiceResult<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<ServiceResult<ProductResponse>> UpdateAsync(
        string id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ServiceResult<ProductResponse>.Fail(
                new Error("Product.InvalidId", "Product id is required."));
        }

        var validationErrors = ValidateProductInput(
            request.Name,
            request.Description,
            request.Price,
            request.Stock);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<ProductResponse>.ValidationFail(validationErrors);
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ServiceResult<ProductResponse>.Fail(
                new Error("Product.NotFound", "Product was not found."));
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;

        var updated = await _productRepository.UpdateAsync(product, cancellationToken);

        if (!updated)
        {
            return ServiceResult<ProductResponse>.Fail(
                new Error("Product.UpdateFailed", "Product could not be updated."));
        }

        _logger.LogInformation("Product updated. ProductId: {ProductId}", product.Id);

        return ServiceResult<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<ServiceResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ServiceResult.Fail(
                new Error("Product.InvalidId", "Product id is required."));
        }

        var deleted = await _productRepository.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return ServiceResult.Fail(
                new Error("Product.NotFound", "Product was not found."));
        }

        _logger.LogInformation("Product deleted. ProductId: {ProductId}", id);

        return ServiceResult.Success();
    }

    private static ProductResponse MapToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.CreatedAt,
            product.UpdatedAt);
    }

    private static Dictionary<string, string[]> ValidateProductInput(
        string name,
        string description,
        decimal price,
        int stock)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Product name is required."];
        }
        else if (name.Length > 100)
        {
            errors["name"] = ["Product name cannot exceed 100 characters."];
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors["description"] = ["Product description is required."];
        }
        else if (description.Length > 500)
        {
            errors["description"] = ["Product description cannot exceed 500 characters."];
        }

        if (price <= 0)
        {
            errors["price"] = ["Product price must be greater than zero."];
        }

        if (stock < 0)
        {
            errors["stock"] = ["Product stock cannot be negative."];
        }

        return errors;
    }
}
```

## ProductService'in sorumluluğu

ProductService business/application logic katmanıdır.

Burada:

- request validation yapılır,
- entity oluşturulur,
- repository çağrılır,
- entity response DTO'ya map edilir,
- business error üretilir,
- operation loglanır.

## Neden endpoint içinde yapmadık?

Endpoint'in görevi HTTP seviyesidir.

Endpoint:

- request alır,
- service çağırır,
- response döner.

Business rule endpoint'e yazılırsa kod büyüdükçe endpoint'ler kirlenir.

## Validation neden burada?

Başlangıç için sade validation yeterli.

Daha gelişmiş projede FluentValidation kullanılabilir.

## Structured logging

Örnek:

```csharp
_logger.LogInformation("Product created. ProductId: {ProductId}", product.Id);
```

String interpolation kullanmadık:

```csharp
_logger.LogInformation($"Product created. ProductId: {product.Id}");
```

Çünkü structured logging sistemleri `{ProductId}` alanını query edilebilir property olarak saklayabilir.

---

# 21. ServiceResult -> HTTP Mapping

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Extensions/ServiceResultExtensions.cs
```

Kod:

```csharp
using MiniCommerce.Shared.Common;

namespace MiniCommerce.Catalog.API.Extensions;

public static class ServiceResultExtensions
{
    public static IResult ToApiResult(this ServiceResult result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return MapFailure(result);
    }

    public static IResult ToApiResult<T>(
        this ServiceResult<T> result,
        Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is null
                ? Results.Ok(result.Data)
                : onSuccess(result.Data!);
        }

        return MapFailure(result);
    }

    private static IResult MapFailure(ServiceResult result)
    {
        if (result.ValidationErrors is not null)
        {
            return Results.ValidationProblem(result.ValidationErrors);
        }

        if (result.Error.Code.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound(new
            {
                error = result.Error
            });
        }

        return Results.BadRequest(new
        {
            error = result.Error
        });
    }
}
```

## Ne işe yarar?

ServiceResult'ı HTTP response'a çevirir.

Mapping:

| ServiceResult | HTTP |
|---|---|
| Success + Data | 200 OK |
| Create Success | 201 Created |
| Success + No Data | 204 No Content |
| ValidationFail | 400 ValidationProblem |
| NotFound | 404 Not Found |
| Business Error | 400 Bad Request |

## Neden bu mapping service içinde değil?

Service layer HTTP bilmemelidir.

Bu ayrım sayesinde ProductService ileride farklı transport ile de kullanılabilir:

- HTTP endpoint
- Message consumer
- Background job
- Test

---

# 22. ProductEndpoints

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Endpoints/ProductEndpoints.cs
```

Kod:

```csharp
using MiniCommerce.Catalog.API.DTOs;
using MiniCommerce.Catalog.API.Extensions;
using MiniCommerce.Catalog.API.Services;

namespace MiniCommerce.Catalog.API.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", async (
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.GetAllAsync(cancellationToken);

            return result.ToApiResult();
        });

        group.MapGet("/{id}", async (
            string id,
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.GetByIdAsync(id, cancellationToken);

            return result.ToApiResult();
        });

        group.MapPost("/", async (
            CreateProductRequest request,
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.CreateAsync(request, cancellationToken);

            return result.ToApiResult(product =>
                Results.Created($"/api/products/{product.Id}", product));
        });

        group.MapPut("/{id}", async (
            string id,
            UpdateProductRequest request,
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.UpdateAsync(id, request, cancellationToken);

            return result.ToApiResult();
        });

        group.MapDelete("/{id}", async (
            string id,
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.DeleteAsync(id, cancellationToken);

            return result.ToApiResult();
        });

        return app;
    }
}
```

## Endpoint'lerin görevi

Endpoint şunları yapar:

1. HTTP route'u tanımlar.
2. Request modelini alır.
3. Service'i çağırır.
4. ServiceResult'ı HTTP response'a çevirir.

Endpoint şunları yapmaz:

- MongoDB sorgusu yazmaz
- Business rule üretmez
- Entity mapping yapmaz
- Validation detayını yönetmez

## `MapGroup` neden kullandık?

Tüm product endpoint'lerini `/api/products` altında toplamak için.

Bu okunabilirlik sağlar.

---

# 23. Program.cs

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Program.cs
```

Kod:

```csharp
using MiniCommerce.Catalog.API.Endpoints;
using MiniCommerce.Catalog.API.Exceptions;
using MiniCommerce.Catalog.API.Extensions;
using MiniCommerce.Catalog.API.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb");

builder.Services.AddCatalogInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapGet("/debug/throw", () =>
    {
        throw new InvalidOperationException("This is a test exception from Catalog.API.");
    });
}

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    Service = "MiniCommerce.Catalog.API",
    Status = "Running"
}));

app.MapProductEndpoints();

app.Run();
```

## Açıklama

### `AddProblemDetails`

ProblemDetails response yapısını desteklemek için eklendi.

### `AddExceptionHandler<GlobalExceptionHandler>`

Global exception handler DI container'a eklendi.

### `UseExceptionHandler`

Exception handling middleware pipeline'a eklendi.

Bu satır olmazsa handler çalışmaz.

### `AddHealthChecks().AddCheck<MongoDbHealthCheck>("mongodb")`

Health check sistemine custom MongoDB check'i eklendi.

### `/debug/throw`

Sadece development ortamında çalışan test endpoint'idir.

Amaç global exception handler'ın çalıştığını doğrulamaktır.

Production'da böyle bir endpoint bırakılmaz.

---

# 24. Global Exception Handler

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/Exceptions/GlobalExceptionHandler.cs
```

Kod:

```csharp
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MiniCommerce.Catalog.API.Exceptions;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}",
            traceId);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Unexpected server error",
            Detail = environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
```

## Ne işe yarar?

Yakalanmamış exception'ları merkezi olarak işler.

## Neden her endpoint'te try/catch yazmadık?

Her endpoint'e try/catch yazmak:

- kod tekrarına yol açar,
- response tutarsızlığı yaratır,
- endpoint'leri kirletir,
- bakım maliyetini artırır.

Global exception handler merkezi ve tutarlı bir hata yönetimi sağlar.

## `traceId` neden önemli?

Client'ın aldığı hata ile server logunu eşleştirmeyi sağlar.

Response içinde görünen:

```json
{
  "traceId": "00-3e8e5bf4131184570fd56f23a21ed3f3..."
}
```

loglarda aranabilir.

## Development ve production farkı

Development ortamında:

```text
exception.Message
```

client'a gösterilir.

Production ortamında:

```text
An unexpected error occurred.
```

gösterilir.

Bunun nedeni güvenliktir. Production'da stack trace, connection string, internal class adı gibi detaylar client'a sızdırılmamalıdır.

---

# 25. MongoDB Health Check

Dosya:

```text
src/services/catalog/MiniCommerce.Catalog.API/HealthChecks/MongoDbHealthCheck.cs
```

Kod:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MiniCommerce.Catalog.API.HealthChecks;

public sealed class MongoDbHealthCheck(IMongoDatabase database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new BsonDocument("ping", 1);

            await database.RunCommandAsync<BsonDocument>(
                command,
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("MongoDB connection is healthy.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "MongoDB connection is unhealthy.",
                exception);
        }
    }
}
```

## Ne işe yarar?

MongoDB'ye `ping` komutu atarak bağlantının sağlıklı olup olmadığını kontrol eder.

## Neden sadece API'nin ayakta olması yeterli değil?

API process'i ayakta olabilir ama MongoDB bağlantısı kopmuş olabilir.

Bu durumda servis gerçekten sağlıklı değildir.

Health check dependency durumunu da kontrol etmelidir.

## Test sonucu

MongoDB çalışırken:

```bash
curl http://localhost:5245/health
```

Beklenen:

```text
Healthy
```

MongoDB durdurulunca:

```bash
docker stop minicomm.mongodb
curl http://localhost:5245/health -i
```

Beklenen:

```text
HTTP/1.1 503 Service Unavailable
Unhealthy
```

---

# 26. Test Komutları

## API çalıştırma

```bash
dotnet run --project src/services/catalog/MiniCommerce.Catalog.API/MiniCommerce.Catalog.API.csproj --urls http://localhost:5245
```

## Root endpoint

```bash
curl http://localhost:5245/
```

Beklenen:

```json
{
  "service": "MiniCommerce.Catalog.API",
  "status": "Running"
}
```

## Health endpoint

```bash
curl http://localhost:5245/health
```

Beklenen:

```text
Healthy
```

## Product listeleme

```bash
curl http://localhost:5245/api/products
```

İlk durumda beklenen:

```json
[]
```

## Product oluşturma

```bash
curl -X POST http://localhost:5245/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MacBook Pro 14",
    "description": "Apple M3 Pro laptop",
    "price": 2500,
    "stock": 10
  }'
```

Beklenen:

```json
{
  "id": "...",
  "name": "MacBook Pro 14",
  "description": "Apple M3 Pro laptop",
  "price": 2500,
  "stock": 10,
  "createdAt": "...",
  "updatedAt": null
}
```

## Product id değişkeni

```bash
PRODUCT_ID="oluşan-product-id"
```

Örnek:

```bash
PRODUCT_ID="6a22929787178fb8a5df6586"
```

## Product detay

```bash
curl http://localhost:5245/api/products/$PRODUCT_ID
```

## Product güncelleme

```bash
curl -X PUT http://localhost:5245/api/products/$PRODUCT_ID \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MacBook Pro 14 Updated",
    "description": "Apple M3 Pro laptop - updated description",
    "price": 2700,
    "stock": 8
  }'
```

Beklenen:

- `name` güncellenir
- `price` güncellenir
- `stock` güncellenir
- `updatedAt` dolu gelir

## Validation testi

```bash
curl -X POST http://localhost:5245/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "",
    "description": "",
    "price": 0,
    "stock": -1
  }'
```

Beklenen:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": [
      "Product name is required."
    ],
    "description": [
      "Product description is required."
    ],
    "price": [
      "Product price must be greater than zero."
    ],
    "stock": [
      "Product stock cannot be negative."
    ]
  }
}
```

## Not found testi

```bash
curl http://localhost:5245/api/products/000000000000000000000000 -i
```

Beklenen:

```http
HTTP/1.1 404 Not Found
```

Body:

```json
{
  "error": {
    "code": "Product.NotFound",
    "message": "Product was not found."
  }
}
```

## Delete testi

```bash
curl -X DELETE http://localhost:5245/api/products/$PRODUCT_ID -i
```

Beklenen:

```http
HTTP/1.1 204 No Content
```

Silindikten sonra tekrar GET:

```bash
curl http://localhost:5245/api/products/$PRODUCT_ID -i
```

Beklenen:

```http
HTTP/1.1 404 Not Found
```

## Exception handler testi

```bash
curl http://localhost:5245/debug/throw -i
```

Beklenen:

```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json
```

Body:

```json
{
  "title": "Unexpected server error",
  "status": 500,
  "detail": "This is a test exception from Catalog.API.",
  "instance": "/debug/throw",
  "traceId": "..."
}
```

## MongoDB health check testi

```bash
docker stop minicomm.mongodb

curl http://localhost:5245/health -i
```

Beklenen:

```http
HTTP/1.1 503 Service Unavailable
```

Body:

```text
Unhealthy
```

MongoDB tekrar başlatma:

```bash
docker start minicomm.mongodb

docker ps --format "table {{.Names}}\t{{.Status}}"

curl http://localhost:5245/health -i
```

Beklenen:

```http
HTTP/1.1 200 OK
```

Body:

```text
Healthy
```

---

# 27. Git Commit

İlk anlamlı commit:

```bash
git add .

git commit -m "feat(catalog): add initial catalog api with mongodb"
```

Kontrol:

```bash
git status
git log --oneline -1
```

Beklenen:

```text
nothing to commit, working tree clean
```

Son commit:

```text
feat(catalog): add initial catalog api with mongodb
```

---

# 28. Bu Session'da Öğrenilen Kavramlar

## Database per Service

Catalog.API kendi MongoDB veritabanını kullanır.

İleride:

- Basket.API Redis kullanacak
- Order.API PostgreSQL kullanacak

Her servis kendi verisinin sahibi olacak.

## Options Pattern

Configuration değerlerini strongly typed class ile okuduk.

## Repository Pattern

MongoDB erişim detaylarını repository katmanına taşıdık.

## Service Layer

Business/application logic endpoint'ten ayrıldı.

## DTO

Entity ile API contract ayrıldı.

## ServiceResult Pattern

Business sonuçları standart hale getirildi.

## Global Exception Handling

Beklenmeyen hatalar merkezi olarak yönetildi.

## Health Check

Servis dependency durumunu kontrol etmeyi öğrendik.

## Structured Logging

Loglarda structured placeholder kullandık:

```csharp
_logger.LogInformation("Product created. ProductId: {ProductId}", product.Id);
```

---

# 29. Production'da Nasıl Gelişirdi?

Bu yapı production'da şu özelliklerle genişletilebilir:

1. FluentValidation
2. API versioning
3. OpenTelemetry tracing
4. Correlation ID middleware
5. Structured JSON logging
6. Centralized logging
7. Unit test
8. Integration test
9. MongoDB index yönetimi
10. Secret management
11. CI/CD pipeline
12. Docker image build
13. Readiness/liveness ayrı health endpoint'leri
14. Authentication/authorization
15. Rate limiting
16. Resilience policy
17. Contract testing
18. Static code analysis
19. Security scanning
20. Container image scanning

---

# 30. Bilinçli Olarak Henüz Yapılmayanlar

Bu session'da aşağıdaki konular bilinçli olarak eklenmedi:

- Basket.API
- Order.API
- Gateway.API
- RabbitMQ publish/consume
- MassTransit
- Message contracts
- Stock reservation
- MongoDB atomic stock update
- Idempotency
- Retry policy
- Error queue
- Outbox Pattern
- API servislerini Dockerize etme
- Postman collection

Bunları ihtiyaç ortaya çıktıkça sırayla ekleyeceğiz.

Bu eğitim açısından daha doğru bir yaklaşımdır çünkü pattern'ler soyut kalmaz; gerçek problem üzerinden öğrenilir.

---

# 31. Bir Sonraki Adım

Bir sonraki ana bölüm Basket.API olacak.

Basket.API içinde şu konular işlenecek:

```text
Basket.API
→ Redis bağlantısı
→ X-Customer-Id header
→ Basket store/cache abstraction
→ Add basket item
→ Update basket item
→ Remove basket item
→ List basket
→ Catalog.API HTTP client ile product doğrulama
```

Daha sonra checkout akışıyla birlikte RabbitMQ ve MassTransit contract'ları ihtiyaç ortaya çıktığında eklenecek.

---

# 32. Session 01 Özet Akış

Bu session'ın teknik akışı:

```text
1. Ortam kontrolü
2. Solution oluşturma
3. Klasör yapısı
4. Shared project
5. Docker Compose altyapısı
6. Infrastructure container'ları
7. Error modeli
8. ServiceResult modeli
9. Catalog.API oluşturma
10. MongoDB options
11. MongoDB DI registration
12. Product entity
13. Product repository
14. Product DTO
15. Product service
16. Validation
17. ServiceResult HTTP mapping
18. Product endpoints
19. Global exception handler
20. MongoDB health check
21. CRUD testleri
22. Git commit
```

Bu akış junior–mid ekip için önemlidir çünkü mikroservis iç yapısı katman katman kurulmuştur.
