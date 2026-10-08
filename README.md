# 🍲 Recipe Manager

A cross-platform mobile & desktop application built with **.NET MAUI (C# / XAML)**, **SQLite**, and **RESTful API Services** to organize recipes, generate shopping lists, and schedule meal plans.

Designed to target both **Windows 11** and **Android mobiles/tablets** from a unified, modern C# codebase.

---

## 📱 Course & Rubric Alignment (MAD / Mobile Application Development)

| Requirement | Implementation Detail |
| :--- | :--- |
| **Mobile Development (Xamarin / .NET MAUI)** | Built with .NET MAUI (the official Microsoft successor to Xamarin.Forms) targeting Windows 11 Desktop (WinUI 3) and Android (API 21+). |
| **Mobile UI/UX Design** | Responsive card layouts, horizontal category chip carousels, hero banners, tab navigation (`AppShell`), dark/light theme support, and custom culinary design system. |
| **RESTful API Integration** | `HttpClient` service consuming **TheMealDB REST API** (`/search.php`, `/categories.php`, `/filter.php`, `/lookup.php`, `/random.php`) with JSON deserialization and resilient offline fallback data. |
| **Local SQLite Data Storage** | `sqlite-net-pcl` asynchronous database engine storing custom recipes, grocery shopping items, scheduled meal plans, and bookmarked favorites locally in `recipemanager_v1.db3`. |
| **Asynchronous Programming** | Non-blocking `async`/`await` patterns for network requests, database transactions, debounced search, and background caching. |
| **Version Control & Solution** | Git repository `.gitignore` configured and Visual Studio solution `RecipeManager.sln` included. |

---

## ✨ Features

### 1. 🔍 Discover Recipes (REST API + Local Search)
- **Live Search**: Instant keyword search for dishes and ingredients (e.g., Pasta, Chicken, Soup).
- **Category Filter**: Filter through categories (*Chicken, Vegetarian, Vegan, Seafood, Pasta, Dessert, Breakfast*).
- **Chef's Pick of the Day**: Spotlight card highlighting a fresh random recipe every session.
- **Recipe Details**: High-resolution meal photography, cuisine origin tag, ingredients list with quantities, step-by-step instructions, and direct YouTube tutorial link.

### 2. 🛒 Smart Shopping List (SQLite CRUD)
- **1-Tap Add from Recipe**: Import all ingredients directly from any recipe into your shopping list.
- **Check-off & Strikethrough**: Interactive check marks for purchased items with live item counters.
- **Quick Manual Entry**: Add custom grocery items with quantities on the fly.
- **Share to Clipboard**: Export and share your formatted shopping list directly via WhatsApp, SMS, or Notes.
- **Clear Done**: One-tap cleanup of completed grocery items.

### 3. 📅 Weekly Meal Planner
- **Day-by-Day Calendar**: Navigate between days with quick jump to Today and Tomorrow.
- **Meal Slots**: Assign dishes to **Breakfast**, **Lunch**, **Dinner**, or **Snack** with color-coded badges.
- **⚡ Auto-Generate Weekly Shopping List**: Scans all recipes scheduled across the week and automatically populates the grocery list with the required ingredients!

### 4. 👨‍🍳 My CookBook & Custom Recipe Creator
- **Secret Family Recipes**: Full creation form with prep time, cook time, servings, ingredients, instructions, and photo URL.
- **Offline Favorites**: Bookmark online recipes for instant offline access without needing an internet connection.
- **Full Database Management**: Edit or delete custom recipes at any time.

---

## 🏗️ Architecture & Project Structure

```
APP_MAD/
├── RecipeManager.sln          # Visual Studio Solution File
├── RecipeManager.csproj       # Project file (Targets Windows 11 & Android)
├── App.xaml / App.xaml.cs     # Application entry and eager DB initialization
├── AppShell.xaml              # TabBar navigation configuration
├── MauiProgram.cs             # Dependency Injection (DI) & service registration
│
├── Models/                    # Data Entities & DTOs
│   ├── Recipe.cs              # Core recipe & ingredient models
│   ├── CustomRecipe.cs        # SQLite entity for user-authored recipes
│   ├── ShoppingItem.cs        # SQLite entity for grocery list
│   ├── MealPlanItem.cs        # SQLite entity for meal scheduler
│   ├── FavoriteRecipe.cs      # SQLite entity for bookmarked recipes
│   └── TheMealDbModels.cs     # REST API JSON response mappers
│
├── Services/                  # Business Logic & Data Access
│   ├── IRecipeApiService.cs   # RESTful API interface
│   ├── RecipeApiService.cs    # REST client (TheMealDB + offline cache)
│   ├── IDatabaseService.cs    # Local SQLite interface
│   └── DatabaseService.cs     # SQLiteAsyncConnection CRUD implementation
│
├── ViewModels/                # MVVM ViewModels (CommunityToolkit.Mvvm)
│   ├── BaseViewModel.cs       # IsBusy, Title, change notifications
│   ├── RecipesViewModel.cs    # Search, category chips, hero card
│   ├── RecipeDetailViewModel.cs# Full recipe, add to list/plan, favorite
│   ├── AddRecipeViewModel.cs  # Custom recipe creation & validation
│   ├── ShoppingListViewModel.cs# Item management, toggle, share, clear
│   ├── MealPlanViewModel.cs   # Date navigator, meal slots, auto-generate list
│   └── FavoritesViewModel.cs  # CookBook & saved offline meals
│
├── Views/                     # XAML User Interface Pages
│   ├── RecipesPage.xaml       # Discover tab
│   ├── RecipeDetailPage.xaml  # Recipe details modal view
│   ├── AddRecipePage.xaml     # Custom recipe creation form
│   ├── ShoppingListPage.xaml  # Shopping list tab
│   ├── MealPlanPage.xaml      # Meal planner tab
│   └── FavoritesPage.xaml     # My CookBook tab
│
├── Converters/                # XAML Value Converters
│   └── ValueConverters.cs     # Strikethrough, meal type colors, boolean inverse
│
└── Resources/                 # Design System & Assets
    ├── AppIcon/               # Gourmet icon assets
    ├── Splash/                # Splash screen assets
    └── Styles/                # Culinary color tokens & UI styles
```

---

## 🚀 How to Run & Download

### 💻 1. On Windows 11

#### Option A: Direct Executable / Downloadable Zip (No build needed)
A pre-compiled standalone package is ready in the project directory:
- **Direct Runnable**: [dist/Windows11/RecipeManager.exe](file:///c:/Users/suhaa/Documents/APP_MAD/dist/Windows11/RecipeManager.exe)
- **Zip Archive**: `RecipeManager-Windows11.zip`
> **Important**: If running from `RecipeManager-Windows11.zip`, right-click the ZIP and choose **Extract All** before opening `RecipeManager.exe`. (Double-clicking an .exe directly inside a zip without extracting causes Windows to show a loading cursor and close because dependent DLLs cannot be loaded from compressed storage).

#### Option B: Run via .NET CLI
In your terminal, navigate to this project folder and run:
```powershell
dotnet run -f net10.0-windows10.0.19041.0
```

#### Option C: Run via Visual Studio
1. Double-click `RecipeManager.sln` to open in Visual Studio.
2. Select target framework `net10.0-windows10.0.19041.0` in the top toolbar.
3. Press **F5** or click **Windows Machine (Run)**.

---

### 📱 2. On Android Mobiles / Tablets

#### Option A: Run via Visual Studio (Recommended)
1. Open `RecipeManager.sln` in **Visual Studio Community**.
2. If prompted, click **Install Android SDK** (Visual Studio configures the JDK and Android SDK automatically).
3. Connect an Android phone via USB (with *USB Debugging* enabled) OR select an Android Emulator.
4. Select `net10.0-android` and press **F5** (Run).

#### Option B: Export Standalone Android APK via CLI
When your Android SDK is configured:
```powershell
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```
The resulting `.apk` file will be generated in `bin/Release/net10.0-android/publish/` and can be downloaded/installed directly onto any Android device!

---

## 🗄️ Database Inspection (SQLite)
The application creates and seeds a local SQLite database named `recipemanager_v1.db3` upon first launch, ensuring offline functionality and initial recipes even with no network connection.
