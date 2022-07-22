import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { PagerComponent } from './components/pager/pager.component';
import { PagingHeaderComponent } from './components/paging-header/paging-header.component';
import {CarouselModule} from 'ngx-bootstrap/carousel'

@NgModule({
  declarations: [


    PagerComponent,
         PagingHeaderComponent
  ],
  imports: [
    CommonModule,
    PaginationModule.forRoot(),
    CarouselModule.forRoot()
  ],
  exports:[
    PaginationModule,
    PagerComponent,
    PagingHeaderComponent,
    CarouselModule
    ]
})
export class SharedModule { }
