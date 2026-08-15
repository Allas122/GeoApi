# GeoApi - Api для хранения и работы с гео-данными.
## Tech Stack:
- **Runtime**: .NET 10.
- **Database**: Postgres+Postgis.
- **ORM**: Dapper
- **Migrations**: FluentMigrator
- **Validation**: FluentValidation
- **Mapping**: Mapperly
- **Tests**: xUnit+Moq+Testcontainers
- **Containerization**: Docker
- **Documentation**: OpenApi+Swagger+Scalar.
## Ключевые возможности:
- Поиск точек в радиусе с фильтрацией.
- Связывание геоточки с ресурсами(Это базовое описание ресурса в виде ltree)
- Масштабирование поиска относительно прямоугольной области.
- Базовая кластеризация точек через сетку.
- Обход дерева ресурсов: поддерево, предки, поиск по lquery-шаблону.
- Пакетное создание точек и ресурсов.
- Курсорная пагинация по id.
- Протухание ресурсов через expires_in.
- Ошибки в формате ProblemDetails(RFC 9457)
## Ограничения:
- `gridSize` у кластеров задаётся в градусах: ячейки уже по метрам на высоких широтах, а кластер, пересекающий 180-й меридиан, получит некорректный центр.
- В кластере отдаётся не больше 100 `resourceIds`; настоящее количество ресурсов - в `resourceCount`.
- Слои разнесены по проектам: GeoApi.Domain(без зависимостей), GeoApi.Application, GeoApi.Infrastructure, GeoApi(хост).
## Запуск:
- Скопировать `.env.example` в `.env` и задать `POSTGRES_PASSWORD` - без него compose не стартует.
- `docker compose up --build` - поднимает Postgis, накатывает миграции, стартует API на 8080.
- Scalar на `/scalar/v1` и OpenApi на `/openapi/v1.json` доступны только при `ASPNETCORE_ENVIRONMENT=Development`(в `.env.example` по умолчанию Production).
## Тесты:
- `dotnet test GeoApi.Tests` - юнит-тесты сервисов, валидаторов и обработчиков ошибок. Работают на моках, ни БД, ни Docker не нужны.
- `dotnet test GeoApi.IntegrationTests` - репозитории и HTTP-слой против настоящего Postgis. **Нужен запущенный Docker**, больше ничего поднимать вручную не надо.
- `dotnet test` - запускает оба проекта разом, то есть тоже требует Docker.
- Интеграционные тесты сами поднимают контейнер `postgis/postgis:17-3.5` на случайном порту, накатывают на него `Migrations/Scripts/001_Initial/Up.sql` и удаляют контейнер после прогона. Compose-стенд и любые ваши локальные базы при этом не затрагиваются.
- Образ качается один раз(~640 МБ), дальше берётся из локального кеша.
## По мере имплементации, Readme будет дополняться
