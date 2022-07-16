import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ShopComponent } from './shop/shop.component';
import { ProductDetailsComponent } from './shop/product-details/product-details.component';
import { RouterModule, Routes } from '@angular/router';

const routes:Routes=[
  {path:'',component:ShopComponent},
  {path:':id',component:ProductDetailsComponent},
];


@NgModule({
  declarations: [],
  imports: [
    RouterModule.forChild(routes)
  ],
  exports:[RouterModule]
})
export class ShopRoutingModule { }
