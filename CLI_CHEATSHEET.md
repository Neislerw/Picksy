# Picksy Photo Culling App - CLI Cheatsheet

## 🚀 Development Commands

### Start Development Server
```bash
npm run dev
```
- Builds the app with webpack in development mode
- Starts Electron with hot reloading
- Opens DevTools automatically

### Start Production Build
```bash
npm start
```
- Runs the pre-built Electron app
- No webpack compilation (faster startup)
- Use after `npm run build`

### Build for Production
```bash
npm run build
```
- Creates optimized production build
- Outputs to `dist/` directory
- Use before `npm start`

## 🛠️ Build Commands

### Webpack Development Build
```bash
npx webpack --mode development
```
- Builds only the webpack bundle
- Does not start Electron
- Useful for debugging build issues

### Webpack Production Build
```bash
npx webpack --mode production
```
- Creates optimized production bundle
- Minifies and optimizes code
- Smaller file sizes

## 🔧 Utility Commands

### Install Dependencies
```bash
npm install
```
- Installs all project dependencies
- Run after cloning or pulling updates

### Update Dependencies
```bash
npm update
```
- Updates packages to latest compatible versions
- Check `package.json` for changes

### Clean Build
```bash
# Remove build artifacts
rm -rf dist/
rm -rf node_modules/
npm install
npm run build
```
- Fresh start when build issues occur
- Clears all cached files

## 🐛 Debugging Commands

### Check for Build Errors
```bash
npm run dev 2>&1 | grep -i error
```
- Shows only error messages from build
- Useful for troubleshooting

### Check Electron Process
```bash
# Windows
tasklist | findstr electron
taskkill /f /im electron.exe

# macOS/Linux
ps aux | grep electron
killall electron
```
- Lists running Electron processes
- Force kills stuck processes

### View Logs
```bash
# Development logs
npm run dev > logs.txt 2>&1

# Check for specific errors
grep -i "error\|failed" logs.txt
```

## 📁 Project Structure Commands

### Key Directories
```
src/
├── main.ts              # Electron main process
├── renderer/            # React app
│   ├── components/      # React components
│   ├── styles/          # CSS files
│   └── App.tsx          # Main React component
├── utils/               # Utility functions
└── types.ts             # TypeScript type definitions

resources/
├── logo.png             # App logo
└── logo.ico             # App icon

dist/                    # Build output
```

## ⚡ Quick Commands

### Development Workflow
```bash
# 1. Start development
npm run dev

# 2. Make changes to code

# 3. Save files (auto-rebuilds)

# 4. Test changes in Electron window
```

### Production Workflow
```bash
# 1. Build for production
npm run build

# 2. Test production build
npm start

# 3. Package for distribution (if needed)
```

## 🚨 Troubleshooting

### Common Issues & Solutions

#### Build Fails
```bash
# Clear cache and rebuild
rm -rf node_modules/
npm install
npm run dev
```

#### Electron Won't Start
```bash
# Kill stuck processes
taskkill /f /im electron.exe  # Windows
killall electron              # macOS/Linux

# Restart development
npm run dev
```

#### TypeScript Errors
```bash
# Check TypeScript compilation
npx tsc --noEmit

# Fix import issues
# Check file paths in imports
```

#### Webpack Asset Issues
```bash
# Clear webpack cache
rm -rf node_modules/.cache/

# Rebuild
npm run dev
```

## 📋 Package.json Scripts Reference

```json
{
  "scripts": {
    "dev": "webpack --mode development && electron .",
    "build": "webpack --mode production",
    "start": "electron .",
    "test": "echo \"Error: no test specified\" && exit 1"
  }
}
```

## 🔍 Environment Variables

### Development
```bash
NODE_ENV=development npm run dev
```

### Production
```bash
NODE_ENV=production npm run build
```

## 📝 Notes

- **Hot Reload**: Changes to React components auto-reload in development
- **Build Time**: First build takes longer, subsequent builds are faster
- **DevTools**: Automatically opens in development mode
- **File Watching**: Webpack watches for file changes and rebuilds automatically
- **Error Handling**: Check console for detailed error messages

## 🎯 Quick Reference

| Command | Purpose | When to Use |
|---------|---------|-------------|
| `npm run dev` | Development with hot reload | Daily development |
| `npm start` | Run production build | Testing final app |
| `npm run build` | Create production build | Before distribution |
| `npm install` | Install dependencies | After clone/pull |
| `taskkill /f /im electron.exe` | Kill stuck processes | When app won't start |

---

*Last updated: Current project version*
*Project: Picksy Photo Culling App* 