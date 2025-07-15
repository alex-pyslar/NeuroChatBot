# ���� ������: ���������� .NET SDK ��� ���������� ����������
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# �������� ����� ������� � ������� ��� �������������� ������������
COPY *.sln .
COPY src/*/*.csproj ./src/
RUN dotnet restore

# �������� ���� �������� ���, ������� .env, � �������� ����������
COPY . .
WORKDIR /app/src
RUN dotnet publish -c Release -o /out --no-restore

# ���� ����������: ���������� ������ ����� ASP.NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# �������� ���������������� ����� � .env �� ����� ������
COPY --from=build /out .
COPY --from=build /app/.env .

# ��������� ���� ��� ���-���������� (���� ���������)
EXPOSE 80

# ������ ���������� ��������� �� .env �����
# ����������: � .NET Core/5+ ������������� ������������ System.Environment ��� IConfiguration ��� ������ .env
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# ��������� ����������
ENTRYPOINT ["dotnet", "NeuroChatBot.dll"]