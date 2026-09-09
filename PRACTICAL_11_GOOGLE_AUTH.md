# Практична робота: використання користувацьких уподобань

## Що реалізовано

- `AuthService` для Google OAuth 2.0 Authorization Code flow з PKCE та `state`.
- Профіль користувача отримується через Google OpenID Connect `userinfo` endpoint.
- `access_token`, `id_token` і `refresh_token` зберігаються через `SecureStorage`.
- Публічні дані профілю (ім'я, email, avatar URL, user id) зберігаються через `Preferences`.
- При наступному запуску `AuthService.InitializeAsync()` використовує refresh token для отримання нового access token.
- `MainActivity` приймає OAuth callback.
- Головна сторінка показує стан входу, ім'я, email та аватар.
- Додавання фільмів, додавання/видалення улюблених та перегляд списку улюблених доступні тільки після входу.
- Перевірка авторизації є не тільки в UI, але й у `MovieService`.

## Налаштування Google OAuth

Ідентифікатор застосунку у проєкті:

`com.companyname.mauistartup`

У `Services/AuthService.cs` замініть:

`YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com`

на Client ID вашого OAuth-клієнта.

Навчальний redirect URI, реалізований відповідно до методички:

`com.companyname.mauistartup:/oauth2redirect`

## Важливо щодо актуального Google OAuth

Методичка використовує custom URI scheme для Android. Актуальні правила Google OAuth для installed apps можуть обмежувати або не підтримувати цей старий варіант redirect URI для Android-клієнтів. Якщо Google Cloud Console відхиляє такий redirect, для реального production-входу слід перейти на актуальний Android Google Identity / Credential Manager flow. Код цієї практичної залишає навчальний callback, оскільки саме його вимагає завдання.

## Перевірка

1. Вказати реальний Client ID.
2. Запустити застосунок.
3. Відкрити сторінку профілю та натиснути `Увійти через Google`.
4. Після входу перевірити ім'я, email та аватар.
5. Відкрити каталог і перевірити, що кнопки улюблених та додавання фільму активні.
6. Перезапустити застосунок і перевірити відновлення входу через refresh token.
7. Натиснути `Вийти з профілю` і перевірити блокування захищених функцій.

Перед здачею видалити `bin` та `obj` з архіву.
