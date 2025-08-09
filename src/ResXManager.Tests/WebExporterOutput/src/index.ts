// src/index.ts

import { WebExportTest } from './resources';
import * as fs from 'fs';
import * as path from 'path';

const webExportTest = new WebExportTest();

// Code refrence tracker should find this line
const resourceKey = 'Some.Special.Key.With.Dots';

console.log(webExportTest.SimpleString);
console.log(webExportTest.WithInterpolation({ voltage: 220, current: 5 }));
console.log(webExportTest.WithBadInterpolation({ voltage: 220, current: 5 }));

// Read and deserialize resources.de.json
const resourcesPath = path.join(__dirname, 'resources.de.json');
const resourcesData = fs.readFileSync(resourcesPath, 'utf8');
const germanResources = JSON.parse(resourcesData);

const webExportTestDE = Object.assign(new WebExportTest(), germanResources.WebExportTest);

console.log(webExportTestDE.SimpleString);
console.log(webExportTestDE.WithInterpolation({ voltage: 220, current: 5 }));
console.log(webExportTestDE.WithBadInterpolation({ voltage: 220, current: 5 }));
